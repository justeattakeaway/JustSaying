using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Interrogation;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal class SubscriptionGroupCollection : ISubscriptionGroupCollection
    {
        private readonly ILogger _logger;
        private readonly IList<ISubscriptionGroup> _buses;

        public SubscriptionGroupCollection(
            IList<ISubscriptionGroup> buses,
            ILogger<SubscriptionGroupCollection> logger)
        {
            _buses = buses;
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

        public SubscriptionGroupsInterrogationResult Interrogate()
        {
            var interrogationResponses = _buses.Select(bus => bus.Interrogate());

            return new SubscriptionGroupsInterrogationResult(interrogationResponses);
        }

        private Task RunImpl(CancellationToken stoppingToken)
        {
            IEnumerable<Task> completionTasks = _buses.Select(bus => bus.Run(stoppingToken)).ToList();

            _logger.LogInformation("Consumer bus successfully started");

            return Task.WhenAll(completionTasks);
        }
    }
}
