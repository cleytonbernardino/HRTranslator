namespace HHub.Shared.Models;

public record WindowNotificationMessage(string Title, string Message) : IAppMessage
{
}
