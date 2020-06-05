using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public class SubscriptionGroupCollection : ISubscriptionGroupCollection
    {
        private readonly ILogger _logger;
        private readonly IList<ISubscriptionGroup> _subscriptionGroups;

        public SubscriptionGroupCollection(
            IList<ISubscriptionGroup> subscriptionGroups,
            ILogger<SubscriptionGroupCollection> logger)
        {
            _subscriptionGroups = subscriptionGroups;
            _logger = logger;
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

        public object Interrogate()
        {
            IEnumerable<object> interrogationResponses = _subscriptionGroups.Select(group => group.Interrogate());
            return new
            {
                SubscriptionGroups = interrogationResponses.ToArray(),
            };
        }

        private Task RunImpl(CancellationToken stoppingToken)
        {
            IEnumerable<Task> completionTasks = _subscriptionGroups.Select(bus => bus.Run(stoppingToken)).ToList();

            _logger.LogInformation("Subscription group collection successfully started");

            return Task.WhenAll(completionTasks);
        }
    }
}
