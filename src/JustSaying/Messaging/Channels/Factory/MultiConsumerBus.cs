using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Factory
{
    internal class MultipleConsumerBus : IConsumerBus
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IConsumerBus> _buses;

        public MultipleConsumerBus(
            IConsumerBusFactory busFactory,
            ILogger<MultipleConsumerBus> logger,
            IConsumerConfig consumerConfig)
        {
            _logger = logger;

            var groups = consumerConfig.ConcurrencyGroupConfiguration.GetAllConcurrencyGroups();

            _buses = groups.Select(busFactory.Create);
        }

        private Task _completion;
        private bool _started;
        private readonly object _startLock = new object();

        public Task Run(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested) return Task.CompletedTask;

            // Double check lock to ensure single-start
            if (_started) return _completion;
            lock (_startLock)
            {
                if (_started) return _completion;

                _completion = RunImpl(stoppingToken);

                _started = true;
                return _completion;
            }
        }

        private Task RunImpl(CancellationToken stoppingToken)
        {
            var completionTasks = _buses.Select(bus => bus.Run(stoppingToken));

            _logger.LogInformation("Consumer bus successfully started");

            return Task.WhenAll(completionTasks);
        }
    }
}
