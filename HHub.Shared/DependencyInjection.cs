using HHub.Shared.DataAccess;
using HHub.Shared.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace HHub.Shared;

public class DependencyInjection
{
    public static void RegisterHHubShared(IServiceCollection services)
    {
        RegisterMessageService(services);
        RegisterDataAccess(services);
    }

    private static void RegisterMessageService(IServiceCollection services)
    {
        services.AddSingleton<IMessageQueue, MessageQueue>();
        services.AddHostedService<MessageQueueService>();
    }

    private static void RegisterDataAccess(IServiceCollection services) => services.AddTransient<ICacheService, DictionayDT>();
}
