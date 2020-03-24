using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal class SubscriptionGroupCollection : ISubscriptionGroup
    {
        private readonly ILogger _logger;
        private readonly IList<ISubscriptionGroup> _buses;

        public SubscriptionGroupCollection(
            ISubscriptionGroupFactory groupFactory,
            IDictionary<string, SubscriptionGroupSettingsBuilder> consumerGroupSettings,
            ILogger<SubscriptionGroupCollection> logger)
        {
            _logger = logger;

            _buses = consumerGroupSettings
                .Values
                .Select(groupFactory.Create).ToList();
        }

        private Task _completion;
        private bool _started;
        private readonly object _startLock = new object();

        public Task Run(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested) return Task.CompletedTask;

            if (!_started)
            {
                lock (_startLock)
                {
                    if (!_started)
                    {
                        _completion = RunImpl(stoppingToken);
                        _started = true;
                    }
                }
            }
            return _completion;
        }

        private Task RunImpl(CancellationToken stoppingToken)
        {
            IEnumerable<Task> completionTasks = _buses.Select(bus => bus.Run(stoppingToken)).ToList();

            _logger.LogInformation("Consumer bus successfully started");

            return Task.WhenAll(completionTasks);
        }
    }
}
