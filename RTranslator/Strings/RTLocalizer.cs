using HHub.Shared.Strings;
using Windows.ApplicationModel.Resources;

namespace RTranslator.Strings;

internal class RTLocalizer : BaseLocalizer
{
    public override ResourceLoader CSharpResourceLoader => new("RTranslator/RTResourcesCSharp");
}
