using JustSaying.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Restaurant.KitchenConsole;

/// <summary>
/// A background service responsible for starting the bus which listens for
/// messages on the configured queues
/// </summary>
public class Subscriber(IMessagingBus bus, ILogger<Subscriber> logger, IMessagePublisher publisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kitchen subscriber running");

        await bus.StartAsync(stoppingToken);
        await publisher.StartAsync(stoppingToken);
    }
}
