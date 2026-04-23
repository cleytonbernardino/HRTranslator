namespace HHub.Shared.Translators;

public interface ITranslator
{
    Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken = default);
    Task<string[]> TranslateBatchAsync(IEnumerable<string> texts, string sourceLang, string targetLang, CancellationToken cancellationToken = default);
}
