using RTranslator.JsonContexts;
using RTranslator.Models;
using System.Text.Json;
using Windows.Storage;

namespace RTranslator.FIleIO;

internal sealed class ExploreItemSerializer
{
    private static readonly string _projectDir = Path.Combine(ApplicationData.Current.LocalFolder.Path, "projects.json");

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static List<ExploreItem> Load()
    {
        if (!File.Exists(_projectDir))
            return [];

        var content = File.ReadAllText(_projectDir);
        if (string.IsNullOrEmpty(content))
            return [];

        try
        {
            return JsonSerializer.Deserialize(content, RTJsonContexts.Default.ListExploreItem) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static void Save(IEnumerable<ExploreItem> projects)
    {
        var json = JsonSerializer.Serialize(projects, _jsonOptions);
        File.WriteAllText(_projectDir, json);
    }

    public static void Save(ExploreItemSelected? project)
    {
        if (project is null)
            return;

        var json = Load();
        var exploreItem = json.FindIndex(item => item.Name == project.Project.Name);
        json[exploreItem] = project.Project;

        var jsonSerialized = JsonSerializer.Serialize(json, _jsonOptions);
        File.WriteAllText(_projectDir, jsonSerialized);
    }
}
