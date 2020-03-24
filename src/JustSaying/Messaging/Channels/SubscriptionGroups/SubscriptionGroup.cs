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
        private readonly IMultiplexer _multiplexer;
        private readonly ICollection<IMultiplexerSubscriber> _consumers;
        private readonly ILogger<SubscriptionGroup> _logger;

        public SubscriptionGroup(
            IEnumerable<IMessageReceiveBuffer> receiveBuffers,
            IMultiplexer multiplexer,
            IEnumerable<IMultiplexerSubscriber> consumers,
            ILogger<SubscriptionGroup> logger)
        {
            _receiveBuffers = receiveBuffers.ToList();
            _multiplexer = multiplexer;
            _consumers = consumers.ToList();
            _logger = logger;
        }

        public Task Run(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Starting up consumer bus with {ConsumerCount} consumers and {ReceiveBuffferCount} receive buffers",
                _consumers.Count,
                _receiveBuffers.Count);

            var completionTasks = new List<Task>();

            completionTasks.AddRange(_receiveBuffers.Select(buffer => buffer.Run(stoppingToken)));
            completionTasks.Add(_multiplexer.Run(stoppingToken));
            completionTasks.AddRange(_consumers.Select(consumer => consumer.Run(stoppingToken)));

            return Task.WhenAll(completionTasks);
        }
    }
}
