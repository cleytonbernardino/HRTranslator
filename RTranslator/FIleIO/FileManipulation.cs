using RTranslator.Models;
using System.Text;

namespace RTranslator.FIleIO;

internal partial class FileManipulation : IAsyncDisposable
{
    const string _OldFormaterIdentifier = "old";
    const int _MinLineLength = 5;

    private IAsyncEnumerator<string>? _iterador;
    private bool _isOpen = false;
    private bool _isBusy = false;

    public void OpenFile(string path)
    {
        var lines = File.ReadLinesAsync(path);
        _iterador = lines.GetAsyncEnumerator();
        _isOpen = true;
    }

    public static void MakeBackup(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath)!;

        string fileName = Path.GetFileName(filePath);
        string newFileName = $"{fileName}.old";

        string fullDestPath = Path.Combine(directory, newFileName);

        File.Copy(filePath, fullDestPath, overwrite: true);
    }

    public async Task<List<Dialogue>> GetContentAsync()
    {
        if (!_isOpen || _isBusy || _iterador is null)
            return [];

        _isBusy = true;

        List<Dialogue> dialogues = new(2500);
        Dialogue currentDialogue = new();
        int processedCount = 0;

        try
        {
            while (true)
            {
                if (!await _iterador.MoveNextAsync())
                {
                    await DisposeAsync();
                    break;
                }
                string line = _iterador.Current.Trim();

                if (IsInvalidLine(line))
                    continue;

                if (line.StartsWith('#'))
                {
                    ProcessComment(line, currentDialogue);
                    continue;
                }

                if (line.StartsWith(_OldFormaterIdentifier, StringComparison.Ordinal))
                {
                    await ProcessOldFormatAsync(line, currentDialogue);
                }
                else
                {
                    ProcessStandardLine(line, currentDialogue);
                }

                dialogues.Add(currentDialogue);
                currentDialogue = new();
                processedCount++;
            }
        }
        finally
        {
            _isBusy = false;
        }

        return dialogues;
    }

    public static async Task SaveChangesAsync(string filePath, List<Dialogue> dialogues)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException();

        var dialogueMap = ConvertDialoguesToDictonary(dialogues);
        var result = new StringBuilder(dialogues.Count * 50);

        bool originalTextFound = false;
        string originalText = string.Empty;
        await foreach (string line in File.ReadLinesAsync(filePath))
        {
            if (line.TrimStart().StartsWith('#') && line.IndexOf('"') == -1)
            {
                result.AppendLine(line);
                continue;
            }

            if (!originalTextFound)
            {
                int firstQuote = line.IndexOf('"');
                if (firstQuote == -1)
                {
                    result.AppendLine(line);
                    continue;
                }
                int secondQuote = line.LastIndexOf('"');

                originalText = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                result.AppendLine(line);
                originalTextFound = true;
            }
            else
            {
                if (dialogueMap.TryGetValue(originalText, out string? newText))
                {
                    var before = line.AsSpan(0, line.IndexOf('"') + 1);
                    result.Append(before).Append(newText).Append('"').AppendLine();
                }
                else
                {
                    result.AppendLine(line);
                }
                originalTextFound = false;
            }
        }
        File.WriteAllText(filePath, result.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_iterador != null)
        {
            await _iterador.DisposeAsync();
            _iterador = null;
        }
        _isOpen = false;
    }

    private static void ProcessComment(string line, Dialogue dialogue)
    {
        if (TryGetNumberLine(line, out int numLine))
        {
            dialogue.Line = numLine;
            return;
        }

        if (TryGetContentInQuotes(line, out var content))
        {
            dialogue.Original = content.ToString();
        }
    }

    private async Task ProcessOldFormatAsync(string currentLine, Dialogue dialogue)
    {
        if (TryGetContentInQuotes(currentLine, out var original))
        {
            dialogue.Original = original.ToString();
        }

        if (await _iterador!.MoveNextAsync())
        {
            string nextLine = _iterador.Current.Trim();
            if (TryGetContentInQuotes(nextLine, out var newText))
            {
                dialogue.New = newText.ToString();
            }
            dialogue.IsOld = true;
        }
    }

    private static void ProcessStandardLine(string line, Dialogue dialogue)
    {
        int firstQuote = line.IndexOf('"');
        if (firstQuote > 0)
        {
            dialogue.Person = line.AsSpan(0, firstQuote).Trim().ToString();
        }

        if (TryGetContentInQuotes(line, out var content))
        {
            dialogue.New = content.ToString();
        }
    }

    private static bool TryGetContentInQuotes(string text, out ReadOnlySpan<char> content)
    {
        int first = text.IndexOf('"');
        int last = text.LastIndexOf('"');

        if (first != -1 && last != -1 && last > first)
        {
            content = text.AsSpan(first + 1, last - first - 1);
            return true;
        }

        content = default;
        return false;
    }

    private static bool TryGetNumberLine(string line, out int numLine)
    {
        numLine = 0;
        if (line.Contains("game", StringComparison.Ordinal))
        {
            int lastColonIndex = line.LastIndexOf(':');
            if (lastColonIndex != -1 && lastColonIndex < line.Length - 1)
            {
                return int.TryParse(line.AsSpan(lastColonIndex + 1), out numLine);
            }
        }
        return false;
    }

    private static bool IsInvalidLine(string line)
    {
        if (string.IsNullOrEmpty(line) || line.Length < _MinLineLength)
            return true;

        return line.Contains(':', StringComparison.Ordinal)
            && line.Contains("translate", StringComparison.Ordinal);
    }
    private static Dictionary<string, string> ConvertDialoguesToDictonary(List<Dialogue> dialogues)
    {
        Dictionary<string, string> dialogueMap = new(dialogues.Count);
        foreach (var dialogue in dialogues)
        {
            if (!string.IsNullOrWhiteSpace(dialogue.Original))
            {
                dialogueMap.TryAdd(dialogue.Original, dialogue.New);
            }
        }

        return dialogueMap;
    }
}
