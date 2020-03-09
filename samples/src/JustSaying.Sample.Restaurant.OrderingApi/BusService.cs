using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

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
        private readonly IMessagePublisher _publisher;

        public BusService(IMessagingBus bus, ILogger<BusService> logger, IMessagePublisher publisher)
        {
            _bus = bus;
            _logger = logger;
            _publisher = publisher;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ordering API subscriber running");

            _bus.Start(stoppingToken);

            var consumerBus = (IAmJustInterrogating) _bus;
            var publisherBus = (IAmJustInterrogating) _publisher;

            var subscribers = consumerBus.WhatDoIHave();
            var publishers = publisherBus.WhatDoIHave();

            Log.Information("WhatDoIHave: {@IHaveConsumers}. {@IHaveSubscribers}",
                subscribers, publishers);

            return Task.CompletedTask;
        }
    }
}
