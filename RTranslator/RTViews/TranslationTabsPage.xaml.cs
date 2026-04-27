using CommunityToolkit.Mvvm.DependencyInjection;
using HHub.Shared.Messages;
using HHub.Shared.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RTranslator.Extensions;
using RTranslator.Models;
using RTranslator.ModelView;
using RTranslator.PanelView;
using RTranslator.PanelViews;
using RTranslator.Strings;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;


namespace RTranslator.RTViews;

public sealed partial class TranslationTabsPage : Page
{
    private TranslationTabsPageViewModel ViewModel { get; }

    private readonly RTLocalizer _localizer = new();
    private readonly IMainApp _mainApp;
    private readonly IMessageQueue _messageQueue;

    private readonly Dictionary<string, int> _tabs = [];

    public TranslationTabsPage()
    {
        ViewModel = Ioc.Default.GetRequiredService<TranslationTabsPageViewModel>();


        InitializeComponent();
        _mainApp = Ioc.Default.GetRequiredService<IMainApp>();
        _messageQueue = Ioc.Default.GetRequiredService<IMessageQueue>();

        ViewModel.RequestAddTabs += OnAddTabRequested;
        ViewModel.RequestRebuildTabs += OnRebuildTabsRequested;
    }

    private static TabViewItem CreateNewTab(string header, Frame frame)
    {
        TabViewItem tabItem = new()
        {
            Header = header,
            IconSource = new SymbolIconSource() { Symbol = Symbol.Document },
            Content = frame
        };
        return tabItem;
    }

    private TabViewItem CreateHomeTab(Frame homeFrame)
    {
        string homeString = _localizer.GetString("HOME");

        var tabItem = CreateNewTab(homeString, homeFrame);
        tabItem.IsClosable = false;
        tabItem.IconSource = new SymbolIconSource() { Symbol = Symbol.Home };

        return tabItem;
    }

    private static Frame CreateNewFrame(Type destination, object? parameter = null)
    {
        Frame frame = new();
        frame.Navigate(destination, parameter);
        return frame;
    }

    private async Task<IReadOnlyList<StorageFile>> PickFilesAsync()
    {
        FileOpenPicker picker = new();
        picker.FileTypeFilter.Add(".rpy");

        var hwnd = WindowNative.GetWindowHandle(_mainApp.GetWindown());
        InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        return files ?? [];
    }

    private async Task<StorageFolder?> PickFolderAsync()
    {
        FolderPicker picker = new();
        picker.FileTypeFilter.Add(".rpy");

        var hwnd = WindowNative.GetWindowHandle(_mainApp.GetWindown());
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder;
    }

    private void OnRebuildTabsRequested()
    {
        if (ViewModel.ExploreItem.Children.Count == 0)
        {
            TabView_AddTabButtonClick(Tab_Root, null!);
        }
        else
        {
            OpenExploreItems();
        }
    }

    private void OnAddTabRequested(ExploreItem project)
    {
        var frame = CreateNewFrame(typeof(TranslationFileComponent), project);
        Tab_Root.TabItems.Add(CreateNewTab(project.Name, frame));
        Tab_Root.SelectedIndex = Tab_Root.TabItems.Count - 1;
        _tabs.TryAdd(project.Name, Tab_Root.SelectedIndex);
    }

    private void OpenExploreItems()
    {
        while (Tab_Root.TabItems.Count > 1)
        {
            Tab_Root.TabItems.RemoveAt(1);
        }

        if (ViewModel.ExploreItem is null)
            return;

        foreach(var file in ViewModel.ExploreItem.GetAllFiles())
        {
            OnAddTabRequested(file);
        }
    }

    private async Task PickManualFiles()
    {
        var files = await PickFilesAsync();
        if (files.Count <= 0) return;
        ViewModel.AddFilesToProject(files);
    }

    private async Task PickAutoFiles()
    {
        var folder = await PickFolderAsync();
        if (folder is null) return;
        ViewModel.AddAllFilesToProject(folder.Path);
    }

    private async void TabView_AddTabButtonClick(TabView sender, object args)
    {
        try
        {
            string description =
                $"{_localizer.GetString("SELECT_FILE_MODE_DESCRIPTION_A")}\n{_localizer.GetString("SELECT_FILE_MODE_DESCRIPTION_M")}";
            var dialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title=_localizer.GetString("SELECT_FILE_MODE_TITLE"),
                Content=description,
                PrimaryButtonText = "Auto",
                SecondaryButtonText = _localizer.GetString("SELECT_FILE_MODE_BUTTON_M"),
                DefaultButton = ContentDialogButton.Secondary
            };
            AlertScreenMessage msg = new(dialog);
            var pickMode = await _messageQueue.EnqueueQuestionAsync(msg);
            if (pickMode == ContentDialogResult.Primary)
                await PickAutoFiles();
            else
                await PickManualFiles();
            ViewModel.SaveExploreItem();
        }
        catch (Exception ex)
        {
            WindowNotificationMessage msg = new(Title: _localizer.GetErroString("FILE_READ"), Message: ex.Message);
            await _messageQueue.EnqueueAsync(msg);
        }
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        string tabName = (string)args.Tab.Header;
        ViewModel.DeleteFile(tabName);
        sender.TabItems.Remove(args.Tab);
        _tabs.Remove(tabName);
    }

    private void TabView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is not TabView tabView)
            return;

        var frame = CreateNewFrame(typeof(SelectProjectComponent));
        tabView.TabItems.Add(CreateHomeTab(frame));
    }

    private void Tab_Root_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        bool isCtrlDown = ctrlState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (isCtrlDown && e.Key == Windows.System.VirtualKey.Tab)
        {
            e.Handled = true;
            ToggleFileSwitcher();
        }
    }

    private void CloseFileSwitcher()
    {
        FileSwitcherOverlay.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }   

    private void ToggleFileSwitcher()
    {
        if (FileSwitcherOverlay.Visibility == Microsoft.UI.Xaml.Visibility.Collapsed)
            FileSwitcherOverlay.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        else
            CloseFileSwitcher();
    }

    private void ChangeFileAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ToggleFileSwitcher();
    }

    private void FileSwitcherOverlay_PointerPressed(object sender, PointerRoutedEventArgs e)
        => CloseFileSwitcher();

    private void FileTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.FirstOrDefault() is not ExploreItem item) return;
        
        if (_tabs.TryGetValue(item.Name, out int index))
        {
            Tab_Root.SelectedIndex = index;
        }
        CloseFileSwitcher();
    }
}
