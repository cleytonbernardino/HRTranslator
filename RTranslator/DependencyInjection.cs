using Microsoft.Extensions.DependencyInjection;
using RTranslator.Models;
using RTranslator.ModelView;
using RTranslator.ViewModel;

namespace RTranslator;

public class DependencyInjection
{
    public static void RegisterRTranslator(IServiceCollection services)
    {
        ResgisterViewModel(services);
        RegisterExploreItem(services);
    }

    private static void ResgisterViewModel(IServiceCollection services)
    {
        services.AddTransient<TranslationTabsPageViewModel>(); // TEMP
        services.AddTransient<SelectProjectViewModel>();
        services.AddTransient<TranslateContentViewModel>();
    }

    private static void RegisterExploreItem(IServiceCollection services) => services.AddSingleton<ExploreItemSelected>();
}
