using Microsoft.UI.Xaml.Controls;

namespace HHub.Shared.Models;

public class AlertScreenMessage : IAppMessage
{
    public ContentDialog Dialog { get; init; }

    public TaskCompletionSource<ContentDialogResult> ResponseSource { get; }

    public AlertScreenMessage() => ResponseSource = new TaskCompletionSource<ContentDialogResult>();

    public AlertScreenMessage(ContentDialog dialog)
    {
        Dialog = dialog;
        ResponseSource = new TaskCompletionSource<ContentDialogResult>();
    }
}
