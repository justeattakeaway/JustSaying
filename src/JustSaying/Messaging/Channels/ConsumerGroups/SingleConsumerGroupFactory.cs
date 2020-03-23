using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    internal class SingleConsumerGroupFactory : IConsumerGroupFactory
    {
        private readonly IMultiplexerFactory _multiplexerFactory;
        private readonly IReceiveBufferFactory _receiveBufferFactory;
        private readonly IChannelDispatcherFactory _channelDispatcherFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SingleConsumerGroupFactory(
            IMultiplexerFactory multiplexerFactory,
            IReceiveBufferFactory receiveBufferFactory,
            IChannelDispatcherFactory channelDispatcherFactory,
            ILoggerFactory loggerFactory)
        {
            _multiplexerFactory = multiplexerFactory ?? throw new ArgumentNullException(nameof(multiplexerFactory));
            _receiveBufferFactory =
                receiveBufferFactory ?? throw new ArgumentNullException(nameof(receiveBufferFactory));
            _channelDispatcherFactory = channelDispatcherFactory ??
                                        throw new ArgumentNullException(nameof(channelDispatcherFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IConsumerGroup Create(ConsumerGroupSettings consumerGroupSettings)
        {
            IReadOnlyList<ISqsQueue> groupQueues = consumerGroupSettings.Queues;

            IMultiplexer multiplexer = _multiplexerFactory.Create(consumerGroupSettings.MultiplexerCapacity);

            var receiveBuffers =
                groupQueues.Select(queue => _receiveBufferFactory.CreateBuffer(queue, consumerGroupSettings))
                    .ToList();

            foreach (IMessageReceiveBuffer receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            var consumers = Enumerable.Range(0, consumerGroupSettings.ConsumerCount)
                .Select(x => _channelDispatcherFactory.Create())
                .ToList();

            foreach (IChannelDispatcher consumer in consumers)
            {
                consumer.DispatchFrom(multiplexer.GetMessagesAsync());
            }

            var bus = new SingleConsumerGroup(
                receiveBuffers,
                multiplexer,
                consumers,
                _loggerFactory.CreateLogger<SingleConsumerGroup>());

            return bus;
        }
    }
}
