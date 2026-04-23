using CommunityToolkit.Mvvm.ComponentModel;
using HHub.Shared.Attributes;
using HHub.Shared.Enums;
using RTranslator.Enums;

namespace RTranslator.Models;

public partial class Dialogue : ObservableObject
{
    [Cache(CacheAttrributeType.Original)]
    public string Original { get; set; } = string.Empty;

    [ObservableProperty]
    [property: Cache(CacheAttrributeType.Translation)]
    private string _new = string.Empty;

    public string Person { get; set; } = string.Empty;
    public int Line { get; set; } = 0;

    [Cache(CacheAttrributeType.Status)]
    public TrustLevel Status { get; set; } = TrustLevel.NotTranslated;

    public bool IsOld = false;
}
