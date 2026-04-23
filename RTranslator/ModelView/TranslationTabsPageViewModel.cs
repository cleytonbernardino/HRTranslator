using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RTranslator.FIleIO;
using RTranslator.Models;
using Windows.Storage;

namespace RTranslator.ModelView;

internal partial class TranslationTabsPageViewModel : ObservableObject, IRecipient<SelectedProjectChangedMessage>
{
    private readonly ExploreItemSelected _exploreItemSelected;

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
            ExploreItem.Children.Add(exploreItem);

            RequestAddTabs?.Invoke(exploreItem);
        }
        ExploreItemSerializer.Save(_exploreItemSelected);
    }

    public void DeleteFile(string fileName)
    {
        var exploreItem = ExploreItem.Children.First(item => item.Name == fileName);
        ExploreItem.Children.Remove(exploreItem);
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
}
