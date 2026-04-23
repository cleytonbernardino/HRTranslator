// Based In => https://github.com/bbepis/XUnity.AutoTranslator/blob/master/src/Translators/GoogleTranslate/GoogleTranslateEndpointV2.cs

using HHub.Shared.Translators;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HHub.Translators;

public sealed partial class GoogleTranslateV2 : ITranslator, IDisposable
{
    // TODO: ajuste se o Google mudar o RPC ID
    private const string TRANSLATE_RPC_ID = "MkEWBc";

    // TODO: ajuste se o Google mudar a versão do servidor
    private const string API_VERSION = "boq_translate-webserver_20210323.10_p0";

    // TODO: troque pelo backend preferido se quiser usar mirror/proxy
    private const string BASE_URL = "https://translate.google.com";

    // ─── Campos internos ───────────────────────────────────────────────────

    // Regex pré-compilada — evita recompilar a cada inicialização
    [GeneratedRegex(@"FdrFJe""\s*:\s*""(-?\d+)""")]
    private static partial Regex FsidRegex();

    private readonly HttpClient _http;
    private readonly CookieContainer _cookies;
    private readonly Random _rng = new();
    private readonly SemaphoreSlim _initLock = new(1, 1); // garante init thread-safe

    private long _fsid;
    private long _reqId;
    private int _translationCount;
    private int _resetAfter;
    private bool _initialized;

    // ─── Construtor ────────────────────────────────────────────────────────

    /// <param name="bypassSslValidation">
    ///   Desativa a validação de certificado SSL.
    ///   Use <c>true</c> apenas em ambientes controlados (ex: proxy local, testes).
    ///   TODO: mantenha <c>false</c> em produção.
    /// </param>
    public GoogleTranslateV2(bool bypassSslValidation = false)
    {
        _cookies = new CookieContainer();

        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        if (bypassSslValidation)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        // Cabeçalhos fixos do cliente (imita Chrome)
        _http.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

        _http.DefaultRequestHeaders.TryAddWithoutValidation("DNT", "1");

        var acceptLangs = new[] { null, "en-US,en;q=0.9", "en-US", "en" };
        var lang = acceptLangs[_rng.Next(acceptLangs.Length)];
        if (lang != null)
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", lang);
    }

    // ─── API pública ───────────────────────────────────────────────────────

    /// <summary>Traduz um único texto.</summary>
    public async Task<string> TranslateAsync(
         string text,
         string sourceLang,
         string targetLang,
         CancellationToken cancellationToken = default)
    {
        var results = await TranslateBatchAsync(new[] { text }, sourceLang, targetLang, cancellationToken);
        return results[0];
    }

    /// <summary>Traduz até 10 textos de uma vez.
    /// Cada texto é enviado em série para preservar o mapeamento 1-para-1
    /// e evitar bloqueio de IP por volume de requests simultâneas.
    /// A cada 15 requests acumuladas (entre chamadas) aplica um delay
    /// aleatório de 0.8–2s para imitar comportamento humano.</summary>
    /// <summary>Traduz até 10 textos de uma vez enviando todos em UMA única requisição HTTP.</summary>
    public async Task<string[]> TranslateBatchAsync(
        IEnumerable<string> texts,
        string sourceLang,
        string targetLang,
        CancellationToken cancellationToken = default)
    {
        var textList = texts?.ToList() ?? throw new ArgumentException("texts não pode ser nulo.");
        if (textList.Count == 0) throw new ArgumentException("texts não pode ser vazio.");
        if (textList.Count > 10) throw new ArgumentException("Máximo de 10 textos por chamada.");

        sourceLang = NormalizeLanguage(sourceLang);
        targetLang = NormalizeLanguage(targetLang);

        // 1. Inicialização e controle de concorrência/banimento
        await EnsureInitializedAsync();
        await EnforceRateLimitsAsync(cancellationToken);

        // 2. Preparar payload (Puro)
        var joinedBatchText = PrepareBatchPayload(textList);

        // 3. Fazer a requisição HTTP
        var request = BuildTranslationRequest(joinedBatchText, sourceLang, targetLang);
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // 4. Atualizar contadores internos pós-sucesso
        UpdateStateAfterRequest();

        // 5. Extrair a resposta bruta
        var translatedJoinedText = ExtractSingleTranslation(body);

        // 6. Limpar e validar o resultado (Puro)
        return ParseBatchResult(translatedJoinedText, textList.Count);
    }

    // ─── Inicialização / obtenção do FSID ─────────────────────────────────

    private async Task EnsureInitializedAsync()
    {
        if (Volatile.Read(ref _initialized)) return;

        await _initLock.WaitAsync();
        try
        {
            if (Volatile.Read(ref _initialized)) return;

            ResetCounters();

            var req = new HttpRequestMessage(HttpMethod.Get, BASE_URL);
            req.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var html = await resp.Content.ReadAsStringAsync();
            var match = FsidRegex().Match(html);

            if (match.Success)
                _fsid = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            else
                Console.WriteLine("[GoogleTranslateV2] FSID não encontrado na página — usando valor aleatório.");

            _initialized = true;
        }
        catch (Exception ex)
        {
            // FSID aleatório já foi definido em ResetCounters(); podemos continuar,
            // mas registramos o erro para que o chamador saiba o que aconteceu.
            Console.WriteLine($"[GoogleTranslateV2] Falha ao obter FSID: {ex.Message} — usando valor aleatório.");
            _initialized = true; // evita loop infinito de retentativas
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ─── Construção da requisição ──────────────────────────────────────────

    private HttpRequestMessage BuildTranslationRequest(
        string batchedText,
        string sourceLang,
        string targetLang)
    {
        var escaped = JsonEscape(JsonEscape(batchedText));

        var postBody = string.Format(
            CultureInfo.InvariantCulture,
            "[[[\"{0}\",\"[[\\\"{1}\\\",\\\"{2}\\\",\\\"{3}\\\",true],[null]]\",null,\"generic\"]]]",
            TRANSLATE_RPC_ID, escaped, sourceLang, targetLang);

        var query = string.Join("&",
            "rpcids=" + TRANSLATE_RPC_ID,
            "f.sid=" + Volatile.Read(ref _fsid).ToString(CultureInfo.InvariantCulture),
            "bl=" + Uri.EscapeDataString(API_VERSION),
            "hl=en-US",
            "soc-app=1",
            "soc-platform=1",
            "soc-device=1",
            "_reqid=" + Volatile.Read(ref _reqId).ToString(CultureInfo.InvariantCulture),
            "rt=c");

        var url = $"{BASE_URL}/_/TranslateWebserverUi/data/batchexecute?{query}";

        var content = new ByteArrayContent(Encoding.UTF8.GetBytes("f.req=" + Uri.EscapeDataString(postBody) + "&"));
        content.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded;charset=UTF-8");

        var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        req.Headers.TryAddWithoutValidation("Referer", BASE_URL + "/");
        req.Headers.TryAddWithoutValidation("X-Same-Domain", "1");
        req.Headers.TryAddWithoutValidation("Origin", BASE_URL);
        req.Headers.TryAddWithoutValidation("Accept", "*/*");

        return req;
    }


    // ─── Extração das traduções ────────────────────────────────────────────

    private static string ExtractSingleTranslation(string responseBody)
    {
        try
        {
            var firstBracket = responseBody.IndexOf('[');
            if (firstBracket < 0)
                throw new InvalidOperationException("Resposta inesperada: nenhum '[' encontrado.");

            var outerJson = ExtractFirstJsonArray(responseBody[firstBracket..]);

            using var outerDoc = JsonDocument.Parse(outerJson);
            var innerJsonRaw = outerDoc.RootElement[0][2].GetString()
                                 ?? throw new InvalidOperationException("JSON interno ausente em [0][2].");

            using var innerDoc = JsonDocument.Parse(innerJsonRaw);

            JsonElement tokens;
            try
            {
                tokens = innerDoc.RootElement[1][0][0][5];
            }
            catch (Exception ex)
            {
                string debugSnippet = innerJsonRaw.Length > 300 ? innerJsonRaw[..300] + "..." : innerJsonRaw;
                throw new InvalidOperationException($"Estrutura mudou — caminho [1][0][0][5] não encontrado. Raw JSON: {debugSnippet}", ex);
            }

            // Concatena todos os tokens na ordem em que vêm,
            // preservando as quebras de linha já presentes em cada token.
            // NÃO pula tokens whitespace-only pois podem ser '\n' necessários.
            var sb = new StringBuilder();
            foreach (var entry in tokens.EnumerateArray())
            {
                var token = entry[0].GetString();
                if (token is null) 
                    continue;

                InjectSpaceIfNeeded(sb, token);
                sb.Append(token);
            }

            return sb.ToString().Trim('\n');
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Falha ao processar resposta do Google Translate: {ex.Message}", ex);
        }
    }

    private static void InjectSpaceIfNeeded(StringBuilder buildText, string tokenText)
    {
        if (buildText.Length <= 0 || string.IsNullOrEmpty(tokenText))
            return;

        char lastChar = buildText[^1];
        char firstChar = tokenText[0];

        if (!char.IsWhiteSpace(lastChar) && !char.IsWhiteSpace(firstChar))
        {
            bool isLastPunctuation = lastChar is '!' or '?' or '.' or ':' or ';';

            // Injetamos um espaço se terminou em pontuação OU se o Google "colou" 
            // duas letras/números que pertencem a tokens diferentes
            if (isLastPunctuation || (char.IsLetterOrDigit(lastChar) && char.IsLetterOrDigit(firstChar)))
            {
                buildText.Append(' ');
            }
        }
    }

    /// <summary>
    /// Extrai o primeiro array JSON balanceado de uma string que pode conter
    /// lixo antes/depois (como o chunk encoding do Google).
    /// </summary>
    private static string ExtractFirstJsonArray(string input)
    {
        int depth = 0;
        bool inString = false;
        bool escape = false;
        int start = input.IndexOf('[');

        if (start < 0)
            throw new InvalidOperationException("Nenhum array JSON encontrado na resposta.");

        for (int i = start; i < input.Length; i++)
        {
            char c = input[i];

            if (escape) { escape = false; continue; }
            if (c == '\\' && inString) { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == '[') depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                    return input[start..(i + 1)];
            }
        }

        throw new InvalidOperationException("Array JSON não fechado na resposta.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Normaliza aliases de idioma para o formato esperado pelo Google.</summary>
    private static string NormalizeLanguage(string lang) => lang switch
    {
        "zh-Hans" or "zh" => "zh-CN",
        "zh-Hant" => "zh-TW",
        _ => lang
    };

    private void ResetCounters()
    {
        Volatile.Write(ref _resetAfter, Random.Shared.Next(75, 125));
        Interlocked.Exchange(ref _translationCount, 0);
        Interlocked.Exchange(ref _reqId, Random.Shared.Next(0, 100_000));

        Interlocked.Exchange(ref _fsid, Math.Abs(Random.Shared.NextInt64()));
    }

    /// <summary>
    /// Escapa uma string para ser embutida dentro de um literal JSON string
    /// (replica o comportamento do JsonHelper.Escape original).
    /// </summary>
    private static string JsonEscape(string s)
    {
        var sb = new StringBuilder(s.Length + 16);
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Aplica delays aleatórios baseados no volume de requisições para evitar IP ban.</summary>
    private async Task EnforceRateLimitsAsync(CancellationToken cancellationToken)
    {
        int currentCount = Volatile.Read(ref _translationCount);
        if (currentCount > 0 && currentCount % 15 == 0)
        {
            await Task.Delay(Random.Shared.Next(800, 2001), cancellationToken);
        }
    }

    /// <summary>Atualiza os contadores de requisição e verifica se o FSID precisa ser resetado.</summary>
    private void UpdateStateAfterRequest()
    {
        Interlocked.Add(ref _reqId, 100_000);
        int newCount = Interlocked.Increment(ref _translationCount);

        if (newCount >= Volatile.Read(ref _resetAfter))
            Volatile.Write(ref _initialized, false);
    }

    /// <summary>Protege as quebras de linha internas convertendo para tags HTML antes do envio.</summary>
    private static string PrepareBatchPayload(List<string> textList)
    {
        // Usamos <br> pois o Google ignora tags HTML e não tenta traduzi-las
        var safeTexts = textList.Select(t => t.Replace("\r\n", "\n").Replace("\n", "<br>"));
        return string.Join("\n\n<br>_|||_<br>\n\n", safeTexts); ;
    }

    [GeneratedRegex(@"\s*(?:<\s*br\s*>)?\s*_\|\|\|_\s*(?:<\s*br\s*>)?\s*")]
    private static partial Regex DelimiterRegex();

    /// <summary>Restaura as quebras de linha e limpa artefatos adicionados pelo tradutor.</summary>
    private static string[] ParseBatchResult(string translatedJoinedText, int expectedCount)
    {
        var delimiterRegex = DelimiterRegex();

        var translatedArray = delimiterRegex.Split(translatedJoinedText);

        if (translatedArray.Length != expectedCount)
        {
            throw new InvalidOperationException(
                $"Descompasso no Batch: Enviamos {expectedCount} textos, mas o Google devolveu {translatedArray.Length} linhas.");
        }

        var finalResults = new string[translatedArray.Length];
        for (int i = 0; i < translatedArray.Length; i++)
        {
            // Às vezes o Google adiciona espaços em volta das tags HTML
            finalResults[i] = translatedArray[i]
                .Trim()
                .Replace("< br >", "\n")
                .Replace("< br>", "\n")
                .Replace("<br >", "\n")
                .Replace("<br>", "\n");
        }

        return finalResults;
    }

    public void Dispose()
    {
        _http.Dispose();
        _initLock.Dispose();
    }
}
