using HHub.ViewModel;
using HHub.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HHub;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        ViewModel = App.Current.Services.GetRequiredService<MainWindowViewModel>();
#if DEBUG
        Title = $"{Title} DEV MODE";
#endif
        Closed += MainWindow_Closed;
    }

    public void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        //ProjectArquiverJson.Save(ExploreItemSelected.Project);
    }

    private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            Frm_Navigation.Navigate(typeof(SettingsView));
            return;
        }
        string? tag = args.InvokedItemContainer.Tag?.ToString();
        if (tag is null)
            return;

        Frm_Navigation.Navigate(typeof(RTranslator.RTViews.TranslationTabsPage));
    }

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        Frm_Navigation.Navigate(typeof(RTranslator.RTViews.TranslationTabsPage));
    }
}
