using CommunityToolkit.WinUI.Collections;
using RTranslator.Enums;
using RTranslator.FIleIO;
using RTranslator.Models;

namespace RTranslator.Extensions;

internal sealed partial class DialogueSource : IIncrementalSource<Dialogue>
{
    private readonly FileManipulation _file = new();

    private List<Dialogue> _searchCache = [];

    private FilterMode _activeFilter = FilterMode.None;
    private List<Dialogue> _filterCache = [];

    private string _searchQuery = string.Empty;

    public List<Dialogue> Dialogues { get; private set; } = [];
    public bool IsLoaded { get; private set; } = false;

    public SearchOptions Options
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _searchCache.Clear();
        }
    } = SearchOptions.None;

    public DialogueSource(string filePath)
    {
        _file.OpenFile(filePath);
        _ = LoadDialoguesAsync();
    }

    private async Task LoadDialoguesAsync()
    {
        try
        {
            Dialogues = await _file.GetContentAsync();
        }finally
        {
            IsLoaded = true;
        }
    }

    public async Task<IEnumerable<Dialogue>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!IsLoaded)
        {
            await WaitToLoad();
            if (!IsLoaded) return [];
        }

        var source = ResolveSource();
        int start = pageIndex * pageSize;
        if (start >= source.Count)
            return [];

        int count = Math.Min(pageSize, source.Count - start);
        return source.GetRange(start, count);
    }

    private async Task WaitToLoad()
    {
        int attemp = 0;
        while (attemp != 3 || !IsLoaded)
        {
            await Task.Delay(100);
            attemp++;
        }
    }

    private List<Dialogue> ResolveSource()
    {
        if (_activeFilter != FilterMode.None)
            return FilterCache();

        if (!string.IsNullOrEmpty(_searchQuery))
            return SearchCache();

        return Dialogues;
    }

    public void SetFilter(FilterMode filter)
    {
        if (_activeFilter == filter) return;

        _activeFilter = filter;
        _filterCache.Clear();

        if (filter != FilterMode.None)
            _searchQuery = string.Empty;
    }

    public void SetQuery(string query)
    {
        if (_searchQuery == query) return;

        _searchCache.Clear();
        _searchQuery = query;
    }

    private List<Dialogue> FilterCache()
    {
        if (_filterCache.Count != 0)
            return _filterCache;

        _filterCache = _activeFilter switch
        {
            FilterMode.Equal => Dialogues.Where(dialogues => dialogues.Original == dialogues.New).ToList(),
            FilterMode.Blank => Dialogues.Where(d => string.IsNullOrEmpty(d.New)).ToList(),
            _ => []
        };

        if (_filterCache.Count == 0)
        {
            _activeFilter = FilterMode.None;
            return Dialogues;
        }

        return _filterCache;
    }

    private List<Dialogue> SearchCache()
    {
        if (_searchCache.Count != 0)
            return _searchCache;

        _searchCache = BuildSearch().ToList();
        return _searchCache;
    }

    private IEnumerable<Dialogue> BuildSearch()
    {
        if (Options.HasFlag(SearchOptions.SearchForLine))
        {
            if (!int.TryParse(_searchQuery, out int line)) return [];
            return Dialogues.Where(dialogue => dialogue.Line == line);
        }

        var comparison = Options.HasFlag(SearchOptions.CaseInsensitive) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;

        Func<Dialogue, string> field = Options.HasFlag(SearchOptions.SearchInTranslated)
            ? static d => d.New
            : static d => d.Original;

        return Options.HasFlag(SearchOptions.SearchInText)
            ? Dialogues.Where(dialogue => field(dialogue).Contains(_searchQuery, comparison))
            : Dialogues.Where(dialogue => field(dialogue).StartsWith(_searchQuery, comparison));
    }

    public void ReplaceInTranslated(string search, string replacement, StringComparison comparison)
    {
        var inCache = _searchCache.Count == 0;
        var replaceIn = inCache ? Dialogues : _searchCache;

        foreach (var dialogue in replaceIn)
        {
            if (dialogue.New.Contains(search, comparison))
            {
                if (inCache)
                {
                    var dialogueIndex = Dialogues.IndexOf(dialogue);
                    Dialogues[dialogueIndex].New = dialogue.New.Replace(search, replacement);
                } else
                {
                    dialogue.New = dialogue.New.Replace(search, replacement);
                }
            }
        }
    }
}
