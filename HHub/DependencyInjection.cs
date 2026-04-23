using HHub.Shared.Translators;
using HHub.Translators;
using HHub.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace HHub;

internal class DependencyInjection
{
    public static void RegisterAppCore(IServiceCollection services)
    {
        RegisterTranslators(services);
        RegisterViewModels(services);
    }

    private static void RegisterTranslators(IServiceCollection services)
    {
        services.AddSingleton<ITranslator, GoogleTranslateV2>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
