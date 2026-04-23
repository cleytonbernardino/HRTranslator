//using AppCore.Models;
//using System.Text.Json;

//namespace AppCore.Traslators;

//internal class GTTraslateService
//{
//    private const string RequestUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:91.0) Gecko/20100101 Firefox/91.0";
//    private const string RequestGoogleTranslatorUrl = @"https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&hl=en&dt=t&dt=bd&dj=1&source=icon&tk=467103.467103&q={2}"; // 0 = src lang, 1 = destLang, 2 = dialogues
//    private const int MaxUrlSize = 2000;

//    private readonly HttpClient _client;
//    private readonly string _srcLang;
//    private readonly string _destLang;

//    public GTTraslateService(string srcLang, string destLang)
//    {
//        _client = new HttpClient();
//        _client.Timeout = TimeSpan.FromSeconds(10);
//        _client.DefaultRequestHeaders.UserAgent.ParseAdd(RequestUserAgent);
//        _srcLang = srcLang;
//        _destLang = destLang;
//    }

//    private string PrepareUrl(string text)
//    {
//        string encondedText = Uri.EscapeDataString(text);
//        string url = string.Format(RequestGoogleTranslatorUrl, _srcLang, _destLang, encondedText);
//        if (url.Length > MaxUrlSize)
//            throw new Exception(); // Trocar para ex personalizada

//        return url;
//    }

//    public async Task<ResponseTranslateResult?> TranslateAsync(string text, CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            var url = PrepareUrl(text);
//            var response = await _client.GetAsync(url, cancellationToken);

//            response.EnsureSuccessStatusCode();

//            string result = await response.Content.ReadAsStringAsync(cancellationToken);
//            return JsonSerializer.Deserialize<ResponseTranslateResult>(result);
//        }
//        catch (HttpRequestException e)
//        {
//            Console.WriteLine($"Erro na requisição: {e.Message}");
//            throw;
//        }
//        catch (Exception)
//        {
//            Console.WriteLine($"Erro desconhecido na requisição");
//            return null;
//        }
//    }
//}
