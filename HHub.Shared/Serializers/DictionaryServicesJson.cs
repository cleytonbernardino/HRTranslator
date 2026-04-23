using HHub.Shared.Attributes;
using HHub.Shared.Enums;
using HHub.Shared.Models;
using HHub.Shared.Strings;
//using HHub.Shared.Utils;
using System.Reflection;
using System.Text.Json;
using Windows.Storage;

namespace HHub.Shared.Serializers;

public class DictionaryServicesJson
{
    private const string CacheDirName = "Dictionaries";

    private readonly string _baseDir = Path.Combine(ApplicationData.Current.LocalFolder.Path, CacheDirName);
    private readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    //private readonly BaseLocalizer _localizer = localizer;


    private static List<CacheModel> TransformInCacheModel<T>(IEnumerable<T> items) where T : class
    {
        List<CacheModel> cacheModels = new(items.Count());

        CacheModel cacheModel = new();
        foreach (T item in items)
        {
            if (item is null)
                continue;

            var type = item.GetType();
            var properties = type.GetProperties();

            if (properties is null)
                continue;

            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute<CacheAttribute>();

                if (attr != null)
                {
                    if (attr.Type == CacheAttrributeType.Original)
                    {
                        cacheModel.Original = property.GetValue(item) as string ?? string.Empty;
                        if (cacheModels.Any(cache => cache.Original == cacheModel.Original))
                        {
                            break;
                        }
                    }
                    else if (attr.Type == CacheAttrributeType.Translation)
                    {
                        cacheModel.Translation = property.GetValue(item) as string ?? string.Empty;
                    }
                }
            }
            if (!string.IsNullOrEmpty(cacheModel.Original) && !string.IsNullOrEmpty(cacheModel.Translation))
            {
                cacheModels.Add(cacheModel);
                cacheModel = new();
            }
        }
        return cacheModels;
    }

    //private static string CalcFileHash(string fileName)
    //{
    //    using var md5 = MD5.Create();

    //    string path = Path.Combine(_baseDir, $"{fileName}.json");
    //    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
    //    using var bufferedStream = new BufferedStream(stream, 1024 * 32);

    //    byte[] hashBytes = md5.ComputeHash(bufferedStream);
    //    return Convert.ToHexStringLower(hashBytes);
    //}

    /// <summary>
    /// Saves a list in a standard format for reuse in other translations.
    /// </summary>
    /// <param name="fileName">The name of the file that will be used for saving. This name can be used to retrieve the cache later.</param>
    /// <param name="items">List that will be saved</param>
    public void Save<T>(string fileName, IEnumerable<T> items) where T : class
    {
        if (!Directory.Exists(_baseDir))
        {
            Directory.CreateDirectory(_baseDir);
        }

        string path = Path.Combine(_baseDir, $"{fileName}.json");
        var cache = TransformInCacheModel(items);

        var json = JsonSerializer.Serialize(cache, _jsonOpts);

        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Returns all the contents of a cache file in a standard format.
    /// </summary>
    /// <param name="fileName">Name of the file where the translation was saved.</param>
    /// <returns>Standard list where the file was saved.</returns>
    /// <exception cref="FileNotFoundException">Launched when the file was not found.</exception>
    public Dictionary<string, CacheModel> Load(string fileName)
    {
        string path = Path.Combine(_baseDir, $"{fileName}.json");
        if (!File.Exists(path))
        {
            Save(fileName, new List<string>(1));
        }
        var file = File.ReadAllText(path);

        var cacheText = JsonSerializer.Deserialize<List<CacheModel>>(file) ?? [];
        var cachedDictionary = new Dictionary<string, CacheModel>();
        foreach (var item in cacheText)
        {
            cachedDictionary.TryAdd(item.Original, item);
        }
        return cachedDictionary;
    }

    public void Move(string OldPath, string newPath)
    {
        //try
        //{
        Directory.Move(OldPath, newPath);
        //}
        //catch (UnauthorizedAccessException)
        //{
        //    Messages.SendMessage("Error", _localizer.GetErroString("WITHOUT_PERMISSION"));
        //}
        //catch (DirectoryNotFoundException)
        //{
        //    Messages.SendMessage("Error", _localizer.GetErroString("DIRECTORY_CANNOT_FOUND"));
        //}
        //catch (Exception)
        //{
        //    Messages.SendMessage("Error", _localizer.GetErroString("UNKNOW_ERRO"));
        //}
    }
}
