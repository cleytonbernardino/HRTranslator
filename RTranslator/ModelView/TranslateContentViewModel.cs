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
    private const int MaxDialoguesForInt = 9;
    private const int MaxReq = 6;
    private const int WaitingTime = 2; // Time in seconds
    #endregion

    #region Events
#nullable enable
    public event Action? TranslateComplete;
#nullable restore
    #endregion

    #region ReadOnly
    private readonly RTLocalizer _localizer;
    private readonly ICacheService _cacheService;
    private readonly Settings _settings;

    private readonly ITranslator _translateService;
    private readonly IMessageQueue _messageQueue;
    #endregion

    #region Private
    private ExploreItem? _exploreItem;
    private bool _isLoaded = false;
    private bool _endDialogues = false;
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
    public partial int DialoguesCount { get; private set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PorcentDialoguesTranslated))]
    public partial int DialoguesMax { get; private set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PorcentDialoguesTranslated))]
    public partial int DialogueProcess { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; } = false;
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
        try
        {
            TagProtector tagProtector = new();
            text = tagProtector.Protect(text);

            var translatedText = await _translateService.TranslateAsync(text, _settings.SrcLang, _settings.DstLang, cancellationToken);
            string result = tagProtector.Restore(translatedText);

            _exploreItem!.TraslatedCount++;
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task TranslateAsync(CancellationToken cancellationToken = default)
    {
        if (!DSource.IsLoaded || IsBusy)
            return;

        IsBusy = true;
        _endDialogues = false;
        var dialogues = Dialogues;
        TagProtector tagProtector = new();

        DialoguesMax = dialogues.Count;
        int nextSaveThreshold = dialogues.Count / 5;
        int requestCounter = 0;
        int currentIndex = 0;

        while (currentIndex < dialogues.Count && !cancellationToken.IsCancellationRequested)
        {
            List<Dialogue> currentBatch;
            if (_settings.UseDic)
            {
                currentBatch = GetNextBatch(dialogues, currentIndex);
            } else
            {
                currentBatch = GetNextBatchWithoutDB(dialogues, currentIndex);
            }

            int currentBatchSize = currentBatch.Count;

            if (_endDialogues)
                break;

            if (currentBatchSize == 0)
            {
                currentIndex += MaxDialoguesForInt;
                continue;
            }

            List<string> dialoguesToTranslate = new(currentBatch.Count);
            foreach(var dialogue in currentBatch)
            {
                if (dialogue.HasLogic)
                {
                    dialoguesToTranslate.Add(tagProtector.Protect(dialogue.TextInIf));
                    dialoguesToTranslate.Add(tagProtector.Protect(dialogue.New));
                }else
                {
                    dialoguesToTranslate.Add(tagProtector.Protect(dialogue.Original));
                }
            }

            try
            {
                var translatedResult = await _translateService.TranslateBatchAsync(dialoguesToTranslate, _settings.SrcLang, _settings.DstLang, cancellationToken);
                ApplyTranslationResults(currentBatch, translatedResult, tagProtector);
            }catch (OperationCanceledException)
            {
                IsBusy = false;
                throw;
            }

            currentIndex += MaxDialoguesForInt;
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

        var selectedToken = GetSearchOptions(tag);
        if (selectedToken == SearchOptions.None) return;

        DSource.Options |= selectedToken;
        _ = DialoguesIncremental!.RefreshAsync();
    }

    public void RemoveSelectedToken(string tag)
    {
        if (IsBusy)
            return;

        var selectedToken = GetSearchOptions(tag);

        DSource.Options &= ~selectedToken;
        _ = DialoguesIncremental!.RefreshAsync();
    }

    public void Filter(string filterTag)
    {
        var filter = GetFilterMode(filterTag);
        DSource.SetFilter(filter);
        _ = DialoguesIncremental!.RefreshAsync();
    }

    private List<Dialogue> GetTranslationInDataBase(List<Dialogue> allDialogues, int currentIndex, int countToTake)
    {
        var partialDialogues = allDialogues.Skip(currentIndex).Take(countToTake).ToList();

        var originalTexts = partialDialogues.Select(d => d.Original).ToList();
        var translated = _cacheService.GetTranslations(originalTexts);
        if (translated.Count == 0) return partialDialogues;

        int partialDialoguesCount = partialDialogues.Count;
        for(int i=0; i < partialDialoguesCount; i++) 
        {
            if (allDialogues[currentIndex].HasLogic)
            {
                currentIndex++;
                continue;
            }

            if (translated.TryGetValue(allDialogues[currentIndex].Original, out var value))
            {
                allDialogues[currentIndex].New = value;
                partialDialogues.Remove(allDialogues[currentIndex]);
            }
            currentIndex++;
        }

        return partialDialogues;
    }

    private List<Dialogue> GetNextBatch(List<Dialogue> allDialogues, int currentIndex)
    {
        int countToTake = Math.Min(MaxDialoguesForInt, allDialogues.Count - currentIndex);

        if (countToTake <= 0)
        {
            _endDialogues = true;
            return [];
        }

        return GetTranslationInDataBase(allDialogues, currentIndex, countToTake); ;
    }

    private List<Dialogue> GetNextBatchWithoutDB(List<Dialogue> allDialogues, int currentIndex)
    {
        int countToTake = Math.Min(MaxDialoguesForInt, allDialogues.Count - currentIndex);

        if (countToTake <= 0)
        {
            _endDialogues = true;
            return [];
        }

        return allDialogues.Skip(currentIndex).Take(countToTake).ToList();
    }

    private static void ApplyTranslationResults(IEnumerable<Dialogue> dialogues, string[] result, TagProtector tagProtector)
    {
        int i = 0;
        foreach (var dialogue in dialogues)
        {
            if (dialogue.HasLogic)
            {
                dialogue.TextInIf = tagProtector.Restore(result[i]);
                i++;
            }
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
        await DialoguesIncremental!.RefreshAsync();
        DialoguesMax = DSource.Dialogues.Count;

        if (!DSource.IsFullyLoaded)
        {
            var waitingTime = TimeSpan.FromSeconds(WaitingTime);
            while (!DSource.IsFullyLoaded)
            {
                await Task.Delay(waitingTime);
                DialoguesMax = DSource.Dialogues.Count;
            }
        }
    }

    private static SearchOptions GetSearchOptions(string name) => Enum.TryParse<SearchOptions>(name, out var result) ? result : SearchOptions.None;
    
    private static FilterMode GetFilterMode(string name) => Enum.TryParse<FilterMode>(name, out var result) ? result : FilterMode.None;

    private void GetDialoguesAlreadyProcessed() => DialogueProcess = _exploreItem!.TraslatedCount;
}
