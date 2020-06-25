using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <inheritdoc />
    public class SubscriptionGroupCollection : ISubscriptionGroup
    {
        private readonly ILogger _logger;
        private readonly IList<ISubscriptionGroup> _subscriptionGroups;

        /// <summary>
        /// Runs multiple instance of <see cref="SubscriptionGroup"/>.
        /// </summary>
        /// <param name="subscriptionGroups">The collection of <see cref="SubscriptionGroup"/> instances to run.</param>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        public SubscriptionGroupCollection(
            IList<ISubscriptionGroup> subscriptionGroups,
            ILogger<SubscriptionGroupCollection> logger)
        {
            _subscriptionGroups = subscriptionGroups ?? throw new System.ArgumentNullException(nameof(subscriptionGroups));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        private Task _completion;
        private bool _started;
        private readonly object _startLock = new object();

        /// <inheritdoc />
        public Task RunAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            if (!_started)
            {
                lock (_startLock)
                {
                    if (!_started)
                    {
                        _completion = RunImplAsync(stoppingToken);
                        _started = true;
                    }
                }
            }

            return _completion;
        }

        /// <inheritdoc />
        public object Interrogate()
        {
            IEnumerable<object> interrogationResponses = _subscriptionGroups.Select(group => group.Interrogate());
            return new
            {
                SubscriptionGroups = interrogationResponses.ToArray(),
            };
        }

        private Task RunImplAsync(CancellationToken stoppingToken)
        {
            IEnumerable<Task> completionTasks = _subscriptionGroups.Select(group => group.RunAsync(stoppingToken)).ToList();

            _logger.LogInformation("Subscription group collection successfully started");

            return Task.WhenAll(completionTasks);
        }
    }
}
