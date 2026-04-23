using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using HHub.Shared.DataAccess;
using HHub.Shared.Enums;
using HHub.Shared.Messages;
using HHub.Shared.Models;
using HHub.Shared.Translators;
using RTranslator.Enums;
using RTranslator.Extensions;
using RTranslator.FIleIO;
using RTranslator.Models;
using RTranslator.Strings;
using RTranslator.Utils;
using System.ComponentModel;
using System.Diagnostics;

namespace RTranslator.ViewModel;

internal sealed partial class TranslateContentViewModel : ObservableObject
{
    #region Const
    private const int MaxDialoguesForInt = 7;
    private const int MaxReq = 6;
    private const int WaitingTime = 2; // Time in seconds
    #endregion

    #region Events
#nullable enable
    public event Action? TranslateComplete;
#nullable restore
    #endregion

    #region ReadOnly
    private readonly HashSet<SearchOptions> _selectedTokens = [];
    private readonly RTLocalizer _localizer;
    private readonly ICacheService _cacheService;
    private readonly Settings _settings;

    private readonly ITranslator _translateService;
    private readonly IMessageQueue _messageQueue;
    #endregion

    #region Private
    private ExploreItem? _exploreItem;
    private bool _isLoaded = false;
    #endregion

    #region Properties
    public DialogueSource DSource { get; private set; }
    public List<Dialogue> Dialogues => DSource.Dialogues;

    public IncrementalLoadingCollection<DialogueSource, Dialogue>? DialoguesIncremental { get; private set; }

    public int PorcentDialoguesTranslated
    {
        get
        {
            if (DialoguesMax == 0) return 0;
            return (int)(((double)DialogueProcess / DialoguesMax) * 100);
        }
    }
    public string FilePath
    {
        get;
        set
        {
            if (!File.Exists(value))
                throw new FileNotFoundException(_localizer.GetErroString("FILE_READ"));

            field = value;
        }
    }
    public string FileName => _exploreItem!.Name;
    public SettingsQuestEnum BackupMode => _settings.BackupMode;
    #endregion

    #region ObservableProperty
    [ObservableProperty]
    public partial int DialoguesCount { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PorcentDialoguesTranslated))]
    public partial int DialoguesMax { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PorcentDialoguesTranslated))]
    public partial int DialogueProcess { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = false;
    #endregion

    public TranslateContentViewModel(
        ITranslator translator,
        Settings settings,
        IMessageQueue messageQueue,
        ICacheService cacheService)
    {
        _translateService = translator;
        _messageQueue = messageQueue;
        _settings = settings;
        _localizer = new RTLocalizer();
        _cacheService = cacheService;
    }


    partial void OnDialogueProcessChanged(int value)
    {
        if (value > DialoguesMax)
            DialogueProcess = DialoguesMax;
    }

    public void Load(ExploreItem item)
    {
        if (_isLoaded)
            return;

        _exploreItem = item;
        FilePath = item.Path!;
        DSource = new(FilePath);
        RegisterDialogues();
        GetDialoguesAlreadyProcessed();

        DialoguesIncremental = new(DSource);
        _isLoaded = true;
    }

    public void BackupFile() => FileManipulation.MakeBackup(FilePath);

    public void SaveCache()
    {
        Dictionary<string, string> cacheDic = [];
        foreach (var dialogue in Dialogues)
        {
            cacheDic.TryAdd(dialogue.Original, dialogue.New);
        }
        _cacheService.Save(cacheDic);
    }

    public void ReadCache()
    {
        var dialogues = Dialogues.Select(d => d.Original);
        var loadedCache = _cacheService.GetTranslations(dialogues);

        if (loadedCache.Count == 0) return;

        foreach (var dialogue in Dialogues)
        {
            if (loadedCache.TryGetValue(dialogue.Original, out var translatedText))
            {
                dialogue.New = translatedText;
            }
        }
    }

    public async void ReplaceText(string search, string replacement, bool caseInsesitive = true)
    {
        if (string.IsNullOrEmpty(search)) return;

        var comparison = caseInsesitive ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;

        DSource.ReplaceInTranslated(search, replacement, comparison);

        await DialoguesIncremental!.RefreshAsync();
    }

    public void OpenFile()
    {
        try
        {
            var process = new ProcessStartInfo(FilePath)
            {
                UseShellExecute = true
            };
            Process.Start(process);
        }
        catch (Win32Exception ex)
        {
            WindowNotificationMessage msg = new(Title: "Error", Message: ex.Message);
            Task.Run(() => _messageQueue.EnqueueAsync(msg));
        }
    }

    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
            return text;

        IsBusy = true;
        TagProtector tagProtector = new();
        text = tagProtector.Protect(text);

        var translatedText = await _translateService.TranslateAsync(text, _settings.SrcLang, _settings.DstLang, cancellationToken);
        string result = tagProtector.Restore(translatedText);

        _exploreItem!.TraslatedCount++;
        IsBusy = false;
        return result;
    }

    public async Task TranslateAsync(CancellationToken cancellationToken = default)
    {
        if (!DSource.IsLoaded || IsBusy)
            return;

        IsBusy = true;
        var dialogues = Dialogues;
        TagProtector tagProtector = new();

        DialoguesMax = dialogues.Count;
        int nextSaveThreshold = dialogues.Count / 5;
        int requestCounter = 0;
        int currentIndex = 0;

        while (currentIndex < dialogues.Count && !cancellationToken.IsCancellationRequested)
        {
            var currentBatch = GetNextBatch(dialogues, currentIndex);
            int currentBatchSize = currentBatch.Count();

            if (currentBatchSize == 0)
                break;

            var texts = currentBatch.Select(e => tagProtector.Protect(e.Original));
            var translatedResult = await _translateService.TranslateBatchAsync(texts, _settings.SrcLang, _settings.DstLang, cancellationToken);

            ApplyTranslationResults(currentBatch, translatedResult, tagProtector);

            currentIndex += currentBatchSize;
            requestCounter++;

            nextSaveThreshold = await HandleThrottlingAndCachingAsync(
                requestCounter,
                currentIndex,
                nextSaveThreshold,
                dialogues
            );

            if (requestCounter > MaxReq)
                requestCounter = 0;
        }

        TranslateComplete?.Invoke();
        IsBusy = false;
    }

    public async Task Save()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _ = Task.Run(async () =>
        {
            var dialogues = Dialogues;
            await FileManipulation.SaveChangesAsync(FilePath, dialogues);
            if (_settings.GenerateCache == SettingsQuestEnum.Always)
                SaveCache();
        });
        IsBusy = false;
    }

    public void SearchFor(string name)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        DSource.SetQuery(name);
        IsBusy = false;
        _ = DialoguesIncremental!.RefreshAsync();
    }

    public void AddSelectedToken(string tag)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        var selectedToken = GetSearchOptions(tag);

        _selectedTokens.Add(selectedToken);
        _ = DialoguesIncremental!.RefreshAsync();
        IsBusy = false;
    }

    public void RemoveSelectedToken(string tag)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        var selectedToken = GetSearchOptions(tag);

        _selectedTokens.Remove(selectedToken);
        _ = DialoguesIncremental!.RefreshAsync();
        IsBusy = false;
    }

    public void Filter(string filterTag)
    {
        var filter = GetFilterMode(filterTag);
        DSource.SetFilter(filter);
        _ = DialoguesIncremental!.RefreshAsync();
    }

    private static IEnumerable<Dialogue> GetNextBatch(List<Dialogue> allDialogues, int currentIndex)
    {
        int countToTake = Math.Min(MaxDialoguesForInt, allDialogues.Count - currentIndex);

        if (countToTake <= 0) return [];

        return allDialogues.Skip(currentIndex).Take(countToTake);
    }

    private static void ApplyTranslationResults(IEnumerable<Dialogue> dialogues, string[] result, TagProtector tagProtector)
    {
        int i = 0;
        foreach (var dialogue in dialogues)
        {
            dialogue.New = tagProtector.Restore(result[i]);
            i++;
        }
    }

    private async Task<int> HandleThrottlingAndCachingAsync(int reqCounter, int currentIndex, int nextSaveThreshold, List<Dialogue> dialogues)
    {
        if (reqCounter > MaxReq)
        {
            DialogueProcess = currentIndex;

            if (currentIndex >= nextSaveThreshold)
            {
                int translatedCount = DialogueProcess + currentIndex;
                var translated = dialogues.Take(translatedCount);

                _exploreItem!.TraslatedCount = DialogueProcess;

                return nextSaveThreshold + (dialogues.Count / 5);
            }

            await Task.Delay(TimeSpan.FromSeconds(WaitingTime));
        }

        return nextSaveThreshold;
    }

    private async void RegisterDialogues()
    {
        while (!DSource.IsLoaded)
        {
            await Task.Delay(200);
        }
        DialoguesMax = DSource.Dialogues.Count;
        if (_settings.GenerateCache == SettingsQuestEnum.Always)
            ReadCache();

    }

    private static SearchOptions GetSearchOptions(string name) => Enum.TryParse<SearchOptions>(name, out var result) ? result : SearchOptions.None;
    
    private static FilterMode GetFilterMode(string name) => Enum.TryParse<FilterMode>(name, out var result) ? result : FilterMode.None;

    private void GetDialoguesAlreadyProcessed() => DialogueProcess = _exploreItem!.TraslatedCount;
}
