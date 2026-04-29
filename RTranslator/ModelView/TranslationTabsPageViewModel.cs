using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RTranslator.Extensions;
using RTranslator.FIleIO;
using RTranslator.Models;
using Windows.Storage;

namespace RTranslator.ModelView;

internal partial class TranslationTabsPageViewModel : ObservableObject, IRecipient<SelectedProjectChangedMessage>
{
    private readonly ExploreItemSelected _exploreItemSelected;

    private readonly List<ExploreItem> _tabsItem = [];

    public ExploreItem ExploreItem => _exploreItemSelected.Project;

    public TranslationTabsPageViewModel(ExploreItemSelected exploreItem)
    {
        WeakReferenceMessenger.Default.Register(this);
        _exploreItemSelected = exploreItem;
    }

    public event Action? RequestRebuildTabs;
    public event Action<ExploreItem>? RequestAddTabs;

    void IRecipient<SelectedProjectChangedMessage>.Receive(SelectedProjectChangedMessage message)
    {
        OnPropertyChanged(nameof(ExploreItem));
        RequestRebuildTabs?.Invoke();
    }

    public void AddFilesToProject(IReadOnlyList<StorageFile> files)
    {
        foreach (var file in files)
        {
            string uniqueName = GetUniqueName(file.Name);
            var exploreItem = new ExploreItem
            {
                Name = uniqueName,
                Path = file.Path,
                IsFile = true
            };
            _tabsItem.Add(exploreItem);
            ExploreItem.Children.Add(exploreItem);

            RequestAddTabs?.Invoke(exploreItem);
        }
    }

    public void AddAllFilesToProject(string startFolder)
    {
        var directories = Directory.EnumerateDirectories(startFolder, "*", SearchOption.AllDirectories);
        var rootFiles = Directory.EnumerateFiles(startFolder, "*.rpy");
        if (rootFiles.Any())
            AddFilesToExploreItem(rootFiles, ExploreItem);

        foreach (var dir in directories)
        {
            var filesPaths = Directory.EnumerateFiles(dir, "*.rpy");
            if (filesPaths.Any())
            {
                var exploreItem = new ExploreItem()
                {
                    Name = Path.GetRelativePath(startFolder, dir),
                    Path = string.Empty,
                    IsFile = false
                };
                AddFilesToExploreItem(filesPaths, exploreItem);

                _tabsItem.Add(exploreItem);
                ExploreItem.Children.Add(exploreItem);
            }
        }
    }

    public void SaveExploreItem() => ExploreItemSerializer.Save(_exploreItemSelected);

    public void DeleteFile(string fileName)
    {
        ExploreItem.RemoveFile(fileName);
        ExploreItemSerializer.Save(_exploreItemSelected);
    }

    private string GetUniqueName(string name)
    {
        if (!ExploreItem.Children.Any(p => p.Name == name))
        {
            return name;
        }

        string nameWithoutExt = Path.GetFileNameWithoutExtension(name);
        string extension = Path.GetExtension(name);

        int counter = 1;
        string newName;

        while (true)
        {
            newName = $"{nameWithoutExt}{counter}{extension}";
            bool exists = ExploreItem.Children.Any(p => p.Name == newName);

            if (!exists)
            {
                return newName;
            }
            counter++;
        }
    }

    private void AddFilesToExploreItem(IEnumerable<string> filesPaths, ExploreItem exploreItem)
    {
        foreach (var filePath in filesPaths)
        {
            string name = Path.GetFileName(filePath);

            ExploreItem exploreItemChild = new()
            {
                Name = GetUniqueName(name),
                Path = filePath,
                IsFile = true
            };
            exploreItem.Children.Add(exploreItemChild);
            _tabsItem.Add(exploreItemChild);
        }
    }
}
