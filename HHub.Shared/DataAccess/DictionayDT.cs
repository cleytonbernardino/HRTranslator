/* 
    This code is not secure; it can be subject to SQL injection attacks. It only serves as a cache for this project.
 */

using Microsoft.Data.Sqlite;

namespace HHub.Shared.DataAccess;

internal sealed partial class DictionayDT : IDisposable, ICacheService
{
    const int MaxItemForBatch = 500;
    const int TranslationInitialBufferSize = 200;

    #region DB FIELDS NAMES
    const string TableName = "Translations";
    const string OriginalFieldName = "OriginalText";
    const string TranslationFieldName = "TranslatedText";
    #endregion

    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    public DictionayDT(string dbPath = "./translation_cache.db")
    {
        _connectionString = $"Data Source={dbPath};Cache=Shared";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        InitializerDataBase();
    }

    private void InitializerDataBase()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {TableName} (
                {OriginalFieldName} TEXT PRIMARY KEY,
                {TranslationFieldName} TEXT NOT NULL
            ) STRICT;";

        command.ExecuteNonQuery();
    }

    public string? GetTranslation(string text)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $@"SELECT {TranslationFieldName} FROM {TableName} WHERE {OriginalFieldName} = {text}";

        var result = command.ExecuteScalar();
        return result as string;
    }

    public Dictionary<string, string> GetTranslations(IEnumerable<string> originalText)
    {
        if (!originalText.Any())
            return [];

        var uniqueKeyText = originalText.Distinct();

        Dictionary<string, string> result = new(TranslationInitialBufferSize);
        var batches = uniqueKeyText.Chunk(MaxItemForBatch);

        foreach (var batch in batches)
        {
            var translationBatch = SearchTranslationInBatch(batch);

            foreach(var translated in translationBatch)
            {
                result.TryAdd(translated.Key, translated.Value);
            }
        }

        return result;
    }

    private Dictionary<string, string> SearchTranslationInBatch(string[] batch)
    {
        Dictionary<string, string> batchResult = new(batch.Length);
        using var command = _connection.CreateCommand();

        List<string> paramNames = new(batch.Length);

        string paramName;
        for (int i=0; i< batch.Length; i++)
        {
            paramName = $"@p{i}";
            paramNames.Add(paramName);

            command.Parameters.AddWithValue(paramName, batch[i]);
        }

        string inClause = string.Join(", ", paramNames);

        command.CommandText = $@"
            SELECT OriginalText, TranslatedText 
            FROM Translations 
            WHERE OriginalText IN ({inClause});";

        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            batchResult[reader.GetString(0)] = reader.GetString(1); 
        }

        return batchResult;
    }

    public void Save(IEnumerable<KeyValuePair<string, string>> cacheToSave)
    {
        var reciverValues = cacheToSave.ToArray();
        if (reciverValues.Length == 0) return;

        using var translation = _connection.BeginTransaction();

        var batches = reciverValues.Chunk(MaxItemForBatch);
        foreach (var batch in batches)
        {
            using var command = CreateBatchSaveCommand(batch, translation);

            command.ExecuteNonQuery();
        }

        translation.Commit();
    }

    private SqliteCommand CreateBatchSaveCommand(KeyValuePair<string, string>[] batch, SqliteTransaction transaction)
    {
        var command = _connection.CreateCommand();
        command.Transaction = transaction;

        List<string> valueClauses = new(batch.Length);

        string originalParam;
        string translatedParam;
        for (int i=0; i< batch.Length; i++)
        {
            originalParam = $"@o{i}";
            translatedParam = $"@t{i}";

            valueClauses.Add($"({originalParam}, {translatedParam})");
            command.Parameters.AddWithValue(originalParam, batch[i].Key);
            command.Parameters.AddWithValue(translatedParam, batch[i].Value);
        }

        string joinedValues = string.Join(", ", valueClauses);
        command.CommandText = $@"
                INSERT INTO {TableName} ({OriginalFieldName}, {TranslationFieldName}) 
                VALUES {joinedValues}
                ON CONFLICT({OriginalFieldName}) DO UPDATE SET 
                    {TranslationFieldName} = excluded.{TranslationFieldName};
            ";

        return command;
    }

    private void Close()
    {
        _connection.Close();
        _connection.Dispose();
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

}
