using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal class SubscriptionGroup : ISubscriptionGroup
    {
        private readonly ICollection<IMessageReceiveBuffer> _receiveBuffers;
        private readonly SubscriptionGroupSettings _settings;
        private readonly IMultiplexer _multiplexer;
        private readonly ICollection<IMultiplexerSubscriber> _subscribers;
        private readonly ILogger<SubscriptionGroup> _logger;

        public SubscriptionGroup(
            SubscriptionGroupSettings settings,
            ICollection<IMessageReceiveBuffer> receiveBuffers,
            IMultiplexer multiplexer,
            ICollection<IMultiplexerSubscriber> subscribers,
            ILogger<SubscriptionGroup> logger)
        {
            _receiveBuffers = receiveBuffers;
            _settings = settings;
            _multiplexer = multiplexer;
            _subscribers = subscribers;
            _logger = logger;
        }

        public Task RunAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Starting up Subscription group with {ReceiveBuffferCount} receive buffers and {SubscriberCount} subscribers ",
                _subscribers.Count,
                _receiveBuffers.Count);

            var completionTasks = new List<Task>();

            completionTasks.AddRange(_receiveBuffers.Select(buffer => buffer.RunAsync(stoppingToken)));
            completionTasks.Add(_multiplexer.RunAsync(stoppingToken));
            completionTasks.AddRange(_subscribers.Select(subscriber => subscriber.RunAsync(stoppingToken)));

            return Task.WhenAll(completionTasks);
        }

        public object Interrogate()
        {
            return new
            {
                _settings.Name,
                ConcurrencyLimit = _subscribers.Count,
                Multiplexer = _multiplexer.Interrogate(),
                ReceiveBuffers = _receiveBuffers.Select(rb => rb.Interrogate()).ToArray(),
            };
        }
    }
}