using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    internal class SingleConsumerGroupFactory : IConsumerGroupFactory
    {
        private readonly ConsumerGroupConfig _consumerGroupConfig;
        private readonly IMultiplexerFactory _multiplexerFactory;
        private readonly ILookup<string, ISqsQueue> _queuesGroupedByConcurrencyGroup;
        private readonly IReceiveBufferFactory _receiveBufferFactory;
        private readonly IChannelDispatcherFactory _channelDispatcherFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SingleConsumerGroupFactory(
            ConsumerGroupConfig consumerGroupConfig,
            ICollection<ISqsQueue> queues,
            IMultiplexerFactory multiplexerFactory,
            IReceiveBufferFactory receiveBufferFactory,
            IChannelDispatcherFactory channelDispatcherFactory,
            ILoggerFactory loggerFactory)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            _consumerGroupConfig = consumerGroupConfig ?? throw new ArgumentNullException(nameof(consumerGroupConfig));
            _multiplexerFactory = multiplexerFactory ?? throw new ArgumentNullException(nameof(multiplexerFactory));
            _receiveBufferFactory =
                receiveBufferFactory ?? throw new ArgumentNullException(nameof(receiveBufferFactory));
            _channelDispatcherFactory = channelDispatcherFactory ?? throw new ArgumentNullException(nameof(channelDispatcherFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _queuesGroupedByConcurrencyGroup =
                queues.ToLookup(queue => _consumerGroupConfig.ConsumerGroupConfiguration
                    .GetConsumerGroupForQueue(queue.QueueName));
        }

        public IConsumerGroup Create(string groupName)
        {
            var consumerGroupSettings = _consumerGroupConfig.ConsumerGroupConfiguration.GetConsumerGroup(groupName);
            var groupQueues = _queuesGroupedByConcurrencyGroup[groupName];

            var multiplexer = _multiplexerFactory.Create(consumerGroupSettings.MultiplexerCapacity);

            var receiveBuffers =
                groupQueues.Select(queue => _receiveBufferFactory.CreateBuffer(queue, consumerGroupSettings))
                    .ToList();

            foreach(var receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            var consumers = Enumerable.Range(0, consumerGroupSettings.ConsumerCount)
                .Select(x => _channelDispatcherFactory.Create())
                .ToList();

            foreach(var consumer in consumers)
            {
                consumer.DispatchFrom(multiplexer.GetMessagesAsync());
            }

            var bus = new SingleConsumerGroup(receiveBuffers, multiplexer, consumers,
                _loggerFactory.CreateLogger<SingleConsumerGroup>());

            return bus;
        }
    }
}
