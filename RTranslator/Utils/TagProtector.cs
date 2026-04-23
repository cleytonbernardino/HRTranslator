using System.Text.RegularExpressions;

namespace RTranslator.Utils;

internal sealed class TagProtector
{
    private const int _MaxTimeInSeconds = 2;

    private readonly Regex _protectRegex = new(
        @"\[(?:\\.|[^\]\\])+\]|\{(?:\\.|[^\}\\])+\}|""",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(_MaxTimeInSeconds));

    private readonly Regex _unprotectRegex = new(
        @"<(\d+)>", RegexOptions.Compiled, TimeSpan.FromSeconds(_MaxTimeInSeconds));

    private readonly char[] _triggers = ['[', '{', '"', '“', '”'];

    private readonly List<string> _storage = new(250);

    private int _lastIndex = 0;

    public string Protect(string text)
    {
        if (text.IndexOfAny(_triggers) == -1)
        {
            return text;
        }

        return _protectRegex.Replace(text, match =>
        {
            _storage.Add(match.Value);

            string token = $"<{_lastIndex}>";
            _lastIndex++;
            return token;
        });
    }

    public string[] Protect(string[] texts)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i] = Protect(texts[i]);
        }
        return texts;
    }

    public string Restore(string text)
    {
        var matches = _unprotectRegex.Matches(text);
        if(matches.Count == 0)
        {
            return text;
        }
        string restoredText = _unprotectRegex.Replace(text, match =>
        {
            if (int.TryParse(match.Groups[1].Value, out int index))
            {
                if (index >= 0 && index < _storage.Count)
                    return _storage[index];
            }
            return match.Value;
        });
        return restoredText;
    }
}
