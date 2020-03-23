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
        private readonly IChannelConsumerFactory _channelConsumerFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SingleConsumerGroupFactory(
            IMultiplexerFactory multiplexerFactory,
            IReceiveBufferFactory receiveBufferFactory,
            IChannelConsumerFactory channelConsumerFactory,
            ILoggerFactory loggerFactory)
        {
            _multiplexerFactory = multiplexerFactory ??
                                  throw new ArgumentNullException(nameof(multiplexerFactory));
            _receiveBufferFactory = receiveBufferFactory ??
                                    throw new ArgumentNullException(nameof(receiveBufferFactory));
            _channelConsumerFactory = channelConsumerFactory ??
                                      throw new ArgumentNullException(nameof(channelConsumerFactory));
            _loggerFactory = loggerFactory ??
                             throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IConsumerGroup Create(ConsumerGroupSettingsBuilder settingsBuilder)
        {
            ConsumerGroupSettings settings = settingsBuilder.Build();

            IReadOnlyList<ISqsQueue> groupQueues = settings.Queues;

            IMultiplexer multiplexer = _multiplexerFactory.Create(settings.MultiplexerCapacity);

            var receiveBuffers =
                groupQueues.Select(queue => _receiveBufferFactory.CreateBuffer(queue, settings))
                    .ToList();

            foreach (IMessageReceiveBuffer receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            var consumers = Enumerable.Range(0, settings.ConsumerCount)
                .Select(x => _channelConsumerFactory.Create())
                .ToList();

            foreach (IChannelConsumer consumer in consumers)
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
