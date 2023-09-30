using JustSaying.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Middleware;

/// <summary>
/// A background service responsible for starting the JustSaying message bus and publisher.
/// </summary>
public class JustSayingService(IMessagingBus bus, IMessagePublisher publisher, ILogger<JustSayingService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await bus.StartAsync(cancellationToken);
        await publisher.StartAsync(cancellationToken);

        logger.LogInformation("Message bus and publisher have started successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
