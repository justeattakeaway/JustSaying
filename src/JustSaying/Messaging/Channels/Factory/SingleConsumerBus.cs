using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Factory
{
    internal class SingleConsumerBus : IConsumerBus
    {
        private readonly ICollection<IMessageReceiveBuffer> _receiveBuffers;
        private readonly IMultiplexer _multiplexer;
        private readonly ICollection<IChannelConsumer> _consumers;
        private readonly ILogger<SingleConsumerBus> _logger;

        public SingleConsumerBus(
            IEnumerable<IMessageReceiveBuffer> receiveBuffers,
            IMultiplexer multiplexer,
            IEnumerable<IChannelConsumer> consumers,
            ILogger<SingleConsumerBus> logger)
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
