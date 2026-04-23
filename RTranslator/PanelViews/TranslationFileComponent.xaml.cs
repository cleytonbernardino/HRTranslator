using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Controls;
using HHub.Shared.Messages;
using HHub.Shared.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RTranslator.Models;
using RTranslator.Strings;
using RTranslator.Utils;
using RTranslator.ViewModel;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RTranslator.PanelView;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class TranslationFileComponent : Page
{
    public TranslateContentViewModel ViewModel { get; private set; }

    private CancellationTokenSource _cancelToken = new();

    private readonly FontIcon _filterSelectedItem = new FontIcon() { Glyph = "\uEB0F" };
    private readonly RTLocalizer _localizer;
    private readonly IMessageQueue _messageQueue;

    private MenuFlyoutItem _lastFilter;

    public TranslationFileComponent()
    {
        ViewModel = Ioc.Default.GetRequiredService<TranslateContentViewModel>();
        _messageQueue = Ioc.Default.GetRequiredService<IMessageQueue>();

        InitializeComponent();
        _lastFilter = Mfi_InicialSelected;
        _lastFilter.Icon = _filterSelectedItem;
        _localizer = new RTLocalizer();

        ViewModel.TranslateComplete += OnTranslationComplete;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ExploreItem exploreItem)
        {
            try
            {
                ViewModel.Load(exploreItem);
            }
            catch (FileNotFoundException ex)
            {
                WindowNotificationMessage msg = new(Title: "Error", Message: ex.Message);
                Task.Run(() => _messageQueue.EnqueueAsync(msg));
            }
        }
    }

    private async void OnTranslationComplete()
    {
        string notificationMsg = $"{_localizer.GetString("TRANSLATION_COMPLETED")}/{ViewModel.FileName}";
        WindowNotificationMessage msg = new(Title: _localizer.GetString("SUCCESS"), Message: notificationMsg);
        await _messageQueue.EnqueueAsync(msg);
    }

    private static bool IsCtrlPressed()
    {
        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        return (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) ==
            Windows.UI.Core.CoreVirtualKeyStates.Down;
    }

    private async Task CancelTask()
    {
        await _cancelToken.CancelAsync();
        _cancelToken = new CancellationTokenSource();
    }

    private async void BtbTranslate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;


        button.IsEnabled = false;
        var context = (Dialogue)button.DataContext;
        context.New = await ViewModel.TranslateAsync(context.Original, _cancelToken.Token);
        button.IsEnabled = true;
    }

    private void BtnRestoreDialogue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        var dataContext = (Dialogue)button.DataContext;
        dataContext.New = dataContext.Original;
    }

    private async void BtnCancelTranslation_Click(object sender, RoutedEventArgs e)
    {
        await CancelTask();
        string notificationMsg = $"{_localizer.GetString("TRANSLATION_CANCELLED")}/{ViewModel.FileName}";
        WindowNotificationMessage msg = new(Title: _localizer.GetString("ALERT"), Message: notificationMsg);
        await _messageQueue.EnqueueAsync(msg);
    }

    private void BtnOpenFile_Click(object sender, RoutedEventArgs e) => ViewModel.OpenFile();

    private async void BtnSaveFile_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.BackupMode == HHub.Shared.Enums.SettingsQuestEnum.Ask)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Title = _localizer.GetString("ALERT"),
                Content = _localizer.GetString("BACKUP_DIALOG"),
                PrimaryButtonText = _localizer.GetString("YES"),
                CloseButtonText = _localizer.GetString("CANCEL"),
                DefaultButton = ContentDialogButton.Close,
            };
            AlertScreenMessage msg = new(dialog);
            var userChoice = await _messageQueue.EnqueueQuestionAsync(msg);

            if (userChoice == ContentDialogResult.Primary)
            {
                ViewModel.BackupFile();
            }
        }
        else if (ViewModel.BackupMode == HHub.Shared.Enums.SettingsQuestEnum.Always)
        {
            ViewModel.BackupFile();
        }
        await ViewModel.Save();
    }

    private async void BtnTranslateAll_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = this.XamlRoot,
            Title = _localizer.GetString("ALERT"),
            Content = _localizer.GetString("TRANSLATE_EVERTHING_CONFIRM_DIALOG"),
            PrimaryButtonText = _localizer.GetString("YES"),
            CloseButtonText = _localizer.GetString("CANCEL"),
            DefaultButton = ContentDialogButton.Primary,
        };
        AlertScreenMessage msg = new(dialog);
        var userChoice = await _messageQueue.EnqueueQuestionAsync(msg);

        if (userChoice == ContentDialogResult.Primary)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                await ViewModel.TranslateAsync(_cancelToken.Token);
                BtnSaveFile_Click(sender, e);
            });
        }
    }

    private void BtnFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem menuItem)
            return;

        string tag = (string)menuItem.Tag;
        if (tag == "All")
        {
            Asb_Search.IsEnabled = true;
        }
        else
        {
            Asb_Search.IsEnabled = false;
        }

        _lastFilter.Icon = null;
        menuItem.Icon = _filterSelectedItem;
        Tbk_Filter.Text = menuItem.Text;
        _lastFilter = menuItem;
        ViewModel.Filter((string)menuItem.Tag);
    }

    private void BtnSaveCache_Click(object sender, RoutedEventArgs e) => ViewModel.SaveCache();
    private void BtnReadCache_Click(object sender, RoutedEventArgs e) => ViewModel.ReadCache();

    private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (IsCtrlPressed())
        {
            if (e.ClickedItem is not Dialogue dialogue)
                return;

            var listView = (ListView)sender;

            listView.IsItemClickEnabled = false;
            string translatedText = await ViewModel.TranslateAsync(dialogue.Original);
            dialogue.New = translatedText;
            listView.IsItemClickEnabled = true;
        }
    }

    private void ListView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
            return;

        Ltv_Main.Focus(FocusState.Keyboard);
    }

    private void Tkv_SearchTokens_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems.FirstOrDefault() is TokenItem addToken)
        {
            string tag = (string)addToken.Tag;
            ViewModel.AddSelectedToken(tag);
        }
        else if (e.RemovedItems.Count > 0 && e.RemovedItems.FirstOrDefault() is TokenItem delToken)
        {
            string tag = (string)delToken.Tag;
            ViewModel.RemoveSelectedToken(tag);
        }
    }

    private void Asb_Search_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.SearchFor(args.QueryText);
    }

    private void KeyboardAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        Asb_Search.Focus(FocusState.Keyboard);
    }

    private void MenuCopyText_Click(object sender, RoutedEventArgs e)
    {
        var menuItem = (MenuFlyoutItem)sender;
        if (menuItem.DataContext is not Dialogue dataContext)
            return;

        ClipboardUtil.SetText(dataContext.New);
    }

    private void MenuCopyOriginalText_Click(object sender, RoutedEventArgs e)
    {
        var menuItem = (MenuFlyoutItem)sender;
        if (menuItem.DataContext is not Dialogue dataContext)
            return;

        ClipboardUtil.SetText(dataContext.Original);
    }

    private void TbxDialogue_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.DialogueProcess++;
    }

    private void BtnUppercase_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem button)
            return;

        var dataContext = (Dialogue)button.DataContext;
        dataContext.New = dataContext.New.ToUpper();
    }

    private void KeyboardAccelerator_Invoked_1(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        Pop_Replace.IsOpen = !Pop_Replace.IsOpen;
    }

    private void ReplaceWindown_ReplaceRequest(object sender, (string replaceText, bool replaceAll) e)
    {
            
    }

    private void ReplaceWindown_CloseRequested(object sender, EventArgs e) => Pop_Replace.IsOpen = false;
}
