using HHub.Shared.Models;
using Microsoft.UI.Xaml.Controls;

namespace HHub.Shared.Messages;

public interface IMessageQueue
{
    Task<ContentDialogResult> EnqueueQuestionAsync(AlertScreenMessage message);
    ValueTask EnqueueAsync(IAppMessage message);

    IAsyncEnumerable<IAppMessage> DequeueAllAsync(CancellationToken stoppingToken);
}
