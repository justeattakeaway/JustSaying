using JustSaying.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Middleware;

/// <summary>
/// A background service responsible for starting the JustSaying message bus and publisher.
/// </summary>
public class JustSayingService : IHostedService
{
    private readonly IMessagingBus _bus;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<JustSayingService> _logger;

    public JustSayingService(IMessagingBus bus, IMessagePublisher publisher, ILogger<JustSayingService> logger)
    {
        _bus = bus;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bus.StartAsync(cancellationToken);
        await _publisher.StartAsync(cancellationToken);

        _logger.LogInformation("Message bus and publisher have started successfully");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
