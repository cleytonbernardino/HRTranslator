using HHub.Shared.Strings;
using Windows.ApplicationModel.Resources;

namespace HHub.Strings;

internal class HHubLocalizer : BaseLocalizer
{
    public override ResourceLoader CSharpResourceLoader => new("HHubResourcesCS");
}
