using HHub.Shared.Messages;
using HHub.Shared.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

internal partial class MessageQueueService(IMessageQueue messageQueue, DispatcherQueue dispatcherQueue) : BackgroundService
{
    private readonly IMessageQueue _messageQueue = messageQueue;
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _messageQueue.DequeueAllAsync(stoppingToken))
        {
            switch (message)
            {
                case WindowNotificationMessage winMsg:
                    ShowWindowsNotification(winMsg);
                    break;

                case AlertScreenMessage alertMsg:
                    await ShowQuestion(alertMsg);
                    break;
            }
        }
    }

    private static void ShowWindowsNotification(WindowNotificationMessage msg)
    {
        var notification = new AppNotificationBuilder()
            .AddText(msg.Title)
            .AddText(msg.Message)
            .BuildNotification();
        AppNotificationManager.Default.Show(notification);
    }

    private async Task ShowQuestion(AlertScreenMessage msg)
    {
        var queueLock = new TaskCompletionSource();
        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                var result = await msg.Dialog.ShowAsync();
                msg.ResponseSource.SetResult(result);
            }catch (Exception ex)
            {
                msg.ResponseSource.SetException(ex);
            }
            finally
            {
                queueLock.SetResult();
            }
        });
        await queueLock.Task;
    }
}
