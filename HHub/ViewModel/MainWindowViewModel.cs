using HHub.Shared.Models;

namespace HHub.ViewModel;

public class MainWindowViewModel
{
    private readonly Settings _settings;

    public bool RTModuleInstalled { get; private set; } = false;

    public MainWindowViewModel(Settings settings)
    {
        _settings = settings;

        VerifyInstalledModules();
    }

    private void VerifyInstalledModules()
    {
        RTModuleInstalled = _settings.RTTranslatorInstalled;
    }
}
