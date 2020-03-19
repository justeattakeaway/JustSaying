using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Factory
{
    internal class SingleConsumerBusFactory : IConsumerBusFactory
    {
        private readonly IConsumerConfig _consumerConfig;
        private readonly IMultiplexerFactory _multiplexerFactory;
        private readonly ILookup<string, ISqsQueue> _queuesGroupedByConcurrencyGroup;
        private readonly IReceiveBufferFactory _receiveBufferFactory;
        private readonly IConsumerFactory _consumerFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SingleConsumerBusFactory(
            IConsumerConfig consumerConfig,
            ICollection<ISqsQueue> queues,
            IMultiplexerFactory multiplexerFactory,
            IReceiveBufferFactory receiveBufferFactory,
            IConsumerFactory consumerFactory,
            ILoggerFactory loggerFactory)
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            _consumerConfig = consumerConfig ?? throw new ArgumentNullException(nameof(consumerConfig));
            _multiplexerFactory = multiplexerFactory ?? throw new ArgumentNullException(nameof(multiplexerFactory));
            _receiveBufferFactory =
                receiveBufferFactory ?? throw new ArgumentNullException(nameof(receiveBufferFactory));
            _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _queuesGroupedByConcurrencyGroup =
                queues.ToLookup(queue => _consumerConfig.ConcurrencyGroupConfiguration
                    .GetConcurrencyGroupForQueue(queue.QueueName));
        }

        public IConsumerBus Create(string groupName)
        {
            var groupConsumerCount = _consumerConfig.ConcurrencyGroupConfiguration.GetConcurrencyForGroup(groupName);
            var groupQueues = _queuesGroupedByConcurrencyGroup[groupName];

            var multiplexer = _multiplexerFactory.Create(_consumerConfig.MultiplexerCapacity);

            var receiveBuffers =
                groupQueues.Select(queue => _receiveBufferFactory.CreateBuffer(queue))
                    .ToList();

            foreach(var receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            var consumers = Enumerable.Range(0, groupConsumerCount)
                .Select(x => _consumerFactory.Create())
                .ToList();

            foreach(var consumer in consumers)
            {
                consumer.ConsumeFrom(multiplexer.GetMessagesAsync());
            }

            var bus = new SingleConsumerBus(receiveBuffers, multiplexer, consumers,
                _loggerFactory.CreateLogger<SingleConsumerBus>());

            return bus;
        }
    }
}
