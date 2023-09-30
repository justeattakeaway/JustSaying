using JustSaying.Messaging;

namespace JustSaying.Sample.Restaurant.OrderingApi;

/// <summary>
/// A background service responsible for starting the bus which listens for
/// messages on the configured queues
/// </summary>
public class BusService(IMessagingBus bus, ILogger<BusService> logger, IMessagePublisher publisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ordering API subscriber running");

        await publisher.StartAsync(stoppingToken);
        await bus.StartAsync(stoppingToken);
    }
}
