using HHub.FIleIO;
using HHub.Shared.Enums;
using HHub.Shared.Models;

namespace HHub.ViewModel;

public class SettingsViewModel(Settings settings)
{

    public Settings Sett { get; } = settings;

    public string[] Languages { get; } = [
    "en",
    "pt",
    "es",
    "fr",
    "de",
    "it",
    "nl",
    "sv",
    "no",
    "da",
    "ru",
    "pl",
    "cs",
    "hu",
    "ro",
    "tr",
    "ar",
    "he",
    "fa",
    "hi",
    "bn",
    "ta",
    "th",
    "zh",
    "ja",
    "ko",
    "vi",
    "id",
    "ms",
    "tl"
];

    public List<SettingsQuestEnum> QuestModes { get; } = Enum.GetValues<SettingsQuestEnum>().Cast<SettingsQuestEnum>().ToList();

    public void Save() => SettingSerializer.Save(Sett);

    public void ChangeBackupMode(object backupMode) => Sett.BackupMode = (SettingsQuestEnum)backupMode;
    
    public void ChangeCacheMode(object cacheMode) => Sett.GenerateCache = (SettingsQuestEnum)cacheMode;
}
