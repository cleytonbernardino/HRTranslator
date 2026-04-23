using HHub.Shared.Models;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Channels;

namespace HHub.Shared.Messages;

internal sealed class MessageQueue : IMessageQueue
{
    private readonly Channel<IAppMessage> _channel;

    public MessageQueue()
    {
        _channel = Channel.CreateUnbounded<IAppMessage>();
    }

    public async ValueTask EnqueueAsync(IAppMessage message) => await _channel.Writer.WriteAsync(message);

    public IAsyncEnumerable<IAppMessage> DequeueAllAsync(CancellationToken stoppingToken) => _channel.Reader.ReadAllAsync(stoppingToken);

    public async Task<ContentDialogResult> EnqueueQuestionAsync(AlertScreenMessage message)
    {
        await EnqueueAsync(message);

        return await message.ResponseSource.Task;
    }
}
