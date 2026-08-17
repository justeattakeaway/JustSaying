using JustSaying;
using Microsoft.Extensions.Hosting;

namespace SampleApp;

/// <summary>Starts the JustSaying bus and keeps it listening until shutdown.</summary>
public sealed class BusRunnerService(IMessagingBus bus) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => bus.StartAsync(stoppingToken);
}
