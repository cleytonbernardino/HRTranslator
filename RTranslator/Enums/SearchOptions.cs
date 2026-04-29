namespace RTranslator.Enums;

[Flags]
public enum SearchOptions
{
    None = 0,
    SearchInText = 1 << 0,
    SearchInOriginal = 1 << 1,
    CaseInsensitive = 1 << 2,
    SearchForLine = 1 << 3,
}
