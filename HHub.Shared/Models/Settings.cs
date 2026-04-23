using HHub.Shared.Enums;

namespace HHub.Shared.Models;

public class Settings
{
    public string SrcLang { get; set; } = "en";
    public string DstLang { get; set; } = "pt";
    public bool UseDic { get; set; } = true;
    public SettingsQuestEnum BackupMode { get; set; } = SettingsQuestEnum.Ask;
    public SettingsQuestEnum GenerateCache { get; set; } = SettingsQuestEnum.Ask;
    public bool RTTranslatorInstalled { get; set; } = false;
}
