using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Restaurant.OrderingApi
{
    /// <summary>
    /// A background service responsible for starting the bus which listens for
    /// messages on the configured queues
    /// </summary>
    public class BusService : BackgroundService
    {
        private readonly IMessagingBus _bus;
        private readonly ILogger<BusService> _logger;

        public BusService(IMessagingBus bus, ILogger<BusService> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ordering API subscriber running");

            await _bus.StartAsync(stoppingToken);
        }
    }
}
