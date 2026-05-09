using CommunityToolkit.Mvvm.ComponentModel;
using HHub.Shared.Attributes;
using HHub.Shared.Enums;

namespace RTranslator.Models;

public partial class Dialogue : ObservableObject
{
    // Base Dialog
    [Cache(CacheAttrributeType.Original)]
    public string Original { get; set; } = string.Empty;

    [ObservableProperty]
    [property: Cache(CacheAttrributeType.Translation)]
    
    public partial string New { get; set; } = string.Empty;
    public string Person { get; set; } = string.Empty;
    public int Line { get; set; } = 0;


    // Dialog with logic
    public bool HasLogic { get; set; } = false;
    public string IfCondition { get; set;  } = string.Empty;
    public string TextInIfOriginal { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TextInIf { get; set; } = string.Empty;
}
