using JustSaying.Messaging;

namespace JustSaying.Sample.Restaurant.OrderingApi;

public sealed class JustSayingHostedService(IMessagingBus bus, IMessagePublisher publisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.WhenAll(publisher.StartAsync(stoppingToken), bus.StartAsync(stoppingToken));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
