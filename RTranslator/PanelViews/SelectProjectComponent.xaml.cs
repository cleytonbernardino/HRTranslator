using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RTranslator.Components;
using RTranslator.Enums;
using RTranslator.Models;
using RTranslator.ModelView;
using RTranslator.Strings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RTranslator.PanelViews;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SelectProjectComponent : Page
{
    private SelectProjectViewModel ViewModel { get; }

    private readonly RTLocalizer _localizer = new();

    public SelectProjectComponent()
    {
        ViewModel = Ioc.Default.GetRequiredService<SelectProjectViewModel>();
        InitializeComponent();
    }

    private async Task<string?> ShowNewProjectDialog()
    {
        TextBoxDialog dialogContent = new()
        {
            XamlRoot = this.XamlRoot,
            Title = _localizer.GetString("ADD_PROJECT_TITLE"),
            PrimaryButtonText = _localizer.GetString("CONFIRM"),
            CloseButtonText = _localizer.GetString("CANCEL"),
            DefaultButton = ContentDialogButton.Primary,
            Text = _localizer.GetString("ADD_PROJECT_DESCRIPTION"),
            PlaceHolder = "Mad Island..."
        };
        var result = await dialogContent.ShowAsync();
        if (result == ContentDialogResult.Primary)
            return dialogContent.GetResultText();
        return null;
    }

    private async Task<bool> ShowDeleteProjectDialog()
    {
        ContentDialog dialog = new()
        {
            XamlRoot = this.XamlRoot,
            Title = _localizer.GetString("DELETE_PROJECT_TITLE"),
            Content = _localizer.GetString("DELETE_PROJECT_CONTENT"),
            PrimaryButtonText = _localizer.GetString("YES"),
            SecondaryButtonText = _localizer.GetString("CANCEL"),
            DefaultButton = ContentDialogButton.Secondary,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async void ShowProjectCreationError()
    {
        ContentDialog dialog = new()
        {
            XamlRoot = this.XamlRoot,
            Title = _localizer.GetString("EXISTING_PROJECT_TITLE"),
            Content = _localizer.GetString("EXISTING_PROJECT_DESCRIPTION"),
            PrimaryButtonText = "OK"
        };

        await dialog.ShowAsync();
    }

    private void SelectProjectCore(object item)
    {
        if (item is not ExploreItem exploreItem)
            return;

        ViewModel.SelectProject(exploreItem);
        WeakReferenceMessenger.Default.Send(new SelectedProjectChangedMessage() { SelectedItem = exploreItem});
    }

    private void SelectProject(object sender, RoutedEventArgs e)
    {
        SelectProjectCore(Trv_Projects.SelectedItem);
    }

    private async void CreatNewProject(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        button.IsEnabled = false;
        var result = await ShowNewProjectDialog();
        if (result is not null)
        {
            bool success = ViewModel.AddProject(result);
            if (success == false)
                ShowProjectCreationError();
        }
        button.IsEnabled = true;
    }

    private async void RefreshView(object sender, RoutedEventArgs e)
    {
        Trv_Projects.ItemsSource = null;
        Trv_Projects.ItemsSource = ViewModel.Projects;
        Btn_ChoiceProject.IsEnabled = false;
        Btn_DeleteProject.IsEnabled = false;
    }

    private async void DeleteProject(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        button.IsEnabled = false;
        if (await ShowDeleteProjectDialog())
        {
            var project = (ExploreItem)Trv_Projects.SelectedItem;
            ViewModel.RemoveProject(project);
            Btn_ChoiceProject.IsEnabled = false;
        }
        else
        {
            button.IsEnabled = true;
        }
    }

    private async void TrvProjects_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.FirstOrDefault() is ExploreItem item)
        {
            if (!item.IsFile)
            {
                Btn_DeleteProject.IsEnabled = true;
            }
            Btn_ChoiceProject.IsEnabled = true;
        }
    }

    private void TrvProjects_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is Grid grid)
        {
            SelectProjectCore(grid.DataContext);
        }
        else if (e.OriginalSource is TextBlock textBlock)
        {
            SelectProjectCore(textBlock.DataContext);
        }
    }
}
