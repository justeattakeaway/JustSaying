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
        private readonly ICollection<IChannelDispatcher> _consumers;
        private readonly ILogger<SingleConsumerGroup> _logger;

        public SingleConsumerGroup(
            IEnumerable<IMessageReceiveBuffer> receiveBuffers,
            IMultiplexer multiplexer,
            IEnumerable<IChannelDispatcher> consumers,
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
                _consumers.Count, _receiveBuffers.Count);

            var bufferTasks = _receiveBuffers.Select(buffer => buffer.Run(stoppingToken));
            var multiplexerTask = _multiplexer.Run(stoppingToken);
            var consumerTasks = _consumers.Select(consumer => consumer.Run(stoppingToken));

            var allTasks = bufferTasks.Concat(consumerTasks).Concat(new[] { multiplexerTask });

            return Task.WhenAll(allTasks);
        }
    }
}
