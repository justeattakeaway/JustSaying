using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Configuration;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    internal class CombinedConsumerGroup : IConsumerGroup
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IConsumerGroup> _buses;

        public CombinedConsumerGroup(
            IConsumerGroupFactory groupFactory,
            ILogger<CombinedConsumerGroup> logger,
            ConsumerGroupConfig consumerGroupConfig)
        {
            _logger = logger;

            var groups = consumerGroupConfig.ConsumerGroupConfiguration.GetAllConcurrencyGroups();

            _buses = groups.Select(groupFactory.Create);
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
