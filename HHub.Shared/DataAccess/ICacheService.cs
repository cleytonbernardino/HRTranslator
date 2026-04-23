namespace HHub.Shared.DataAccess;

public interface ICacheService
{
    string? GetTranslation(string text);
    Dictionary<string, string> GetTranslations(IEnumerable<string> originalText);
    void Save(IEnumerable<KeyValuePair<string, string>> cacheToSave);
}
