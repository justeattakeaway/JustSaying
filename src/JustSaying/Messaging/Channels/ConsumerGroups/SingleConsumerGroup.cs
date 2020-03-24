using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    internal class SingleConsumerGroup : IConsumerGroup
    {
        private readonly ICollection<IMessageReceiveBuffer> _receiveBuffers;
        private readonly IMultiplexer _multiplexer;
        private readonly ICollection<IChannelConsumer> _consumers;
        private readonly ILogger<SingleConsumerGroup> _logger;

        public SingleConsumerGroup(
            IEnumerable<IMessageReceiveBuffer> receiveBuffers,
            IMultiplexer multiplexer,
            IEnumerable<IChannelConsumer> consumers,
            ILogger<SingleConsumerGroup> logger)
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
