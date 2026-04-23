namespace HHub.Shared.Models;

public record CacheModel
{
    public string Original { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
}
