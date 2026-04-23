namespace RTranslator.Models;

internal record SelectedProjectChangedMessage
{
    public ExploreItem SelectedItem { get; init; } = default!;
}
