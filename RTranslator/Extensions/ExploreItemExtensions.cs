using RTranslator.Models;

namespace RTranslator.Extensions;

internal static class ExploreItemExtensions
{
    public static List<ExploreItem> GetAllFiles(this ExploreItem item)
    {
        var files = new List<ExploreItem>();
        CollectFiles(item, files);
        return files;
    }

    public static List<ExploreItem> GetAllFiles(this IEnumerable<ExploreItem> items)
    {
        var files = new List<ExploreItem>();
        foreach (var item in items)
            CollectFiles(item, files);
        return files;
    }

    private static void CollectFiles(ExploreItem item, List<ExploreItem> files)
    {
        if (item.IsFile)
        {
            files.Add(item);
            return;
        }

        foreach (var child in item.Children)
            CollectFiles(child, files);
    }
}
