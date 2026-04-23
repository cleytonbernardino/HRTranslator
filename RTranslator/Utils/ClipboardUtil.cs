using Windows.ApplicationModel.DataTransfer;

namespace RTranslator.Utils;

internal class ClipboardUtil
{
    public static void SetText(string text)
    {
        DataPackage dataPackage = new();
        dataPackage.SetText(text);

        Clipboard.SetContent(dataPackage);
    }
}
