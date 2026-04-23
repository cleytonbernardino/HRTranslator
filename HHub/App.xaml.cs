using CommunityToolkit.Mvvm.DependencyInjection;
using HHub.FIleIO;
using HHub.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HHub;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application, IMainApp
{
    public static Window? Window { get; private set; }

    public IHost AppHost { get; }
    public IServiceProvider Services { get; }

    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        AppHost = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
        {
            services.AddSingleton<IMainApp>(this);
            services.AddSingleton(DispatcherQueue.GetForCurrentThread());

            LoadSettings(services);
            LoadModules(services);
        }).Build();
        Ioc.Default.ConfigureServices(AppHost.Services);
        Services = AppHost.Services;

        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        await AppHost.StartAsync();

        Window.Activate();
    }

    private static void LoadSettings(IServiceCollection services)
    {

        var settings = SettingSerializer.Load();
        services.AddSingleton(settings);
    }

    private static void LoadModules(IServiceCollection services)
    {
        HHub.Shared.DependencyInjection.RegisterHHubShared(services);
        HHub.DependencyInjection.RegisterAppCore(services);
        RTranslator.DependencyInjection.RegisterRTranslator(services);
    }

    Window? IMainApp.GetWindown() => Window;
}
