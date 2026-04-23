using HHub.Shared.Utils;
using RTranslator.Enums;

namespace RTranslator.Models;

public record ExploreItem
{
    public string Name { get; init; } = string.Empty;
    public bool IsFile { get; init; } = false;
    public string? Path { get; init; } = null;
    public List<SearchOptions> LastTokens { get; init; } = [];
    public int TraslatedCount { get; set; }

    public ObservableRangeCollection<ExploreItem> Children { get; init; } = [];
}
