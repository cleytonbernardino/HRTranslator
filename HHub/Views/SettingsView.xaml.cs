using CommunityToolkit.Mvvm.DependencyInjection;
using HHub.ViewModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HHub.Views;
/// <summary>   
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsView : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsView()
    {
        ViewModel = Ioc.Default.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.Save();
        base.OnNavigatedFrom(e);
    }

    private void BackupMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedMode = e.AddedItems[0];
        ViewModel.ChangeBackupMode(selectedMode);
    }

    private void CacheMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedMode = e.AddedItems[0];
        ViewModel.ChangeCacheMode(selectedMode);
    }
}
