using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal class SubscriptionGroupFactory : ISubscriptionGroupFactory
    {
        private readonly IMultiplexerFactory _multiplexerFactory;
        private readonly IReceiveBufferFactory _receiveBufferFactory;
        private readonly IMultiplexerSubscriberFactory _multiplexerSubscriberFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SubscriptionGroupFactory(
            IMultiplexerFactory multiplexerFactory,
            IReceiveBufferFactory receiveBufferFactory,
            IMultiplexerSubscriberFactory multiplexerSubscriberFactory,
            ILoggerFactory loggerFactory)
        {
            _multiplexerFactory = multiplexerFactory ??
                                  throw new ArgumentNullException(nameof(multiplexerFactory));
            _receiveBufferFactory = receiveBufferFactory ??
                                    throw new ArgumentNullException(nameof(receiveBufferFactory));
            _multiplexerSubscriberFactory = multiplexerSubscriberFactory ??
                                      throw new ArgumentNullException(nameof(multiplexerSubscriberFactory));
            _loggerFactory = loggerFactory ??
                             throw new ArgumentNullException(nameof(loggerFactory));
        }

        public ISubscriptionGroup Create(SubscriptionGroupSettingsBuilder settingsBuilder)
        {
            SubscriptionGroupSettings settings = settingsBuilder.Build();

            IReadOnlyCollection<ISqsQueue> groupQueues = settings.Queues;

            IMultiplexer multiplexer = _multiplexerFactory.Create(settings.MultiplexerCapacity);

            var receiveBuffers =
                groupQueues
                    .Select(queue => _receiveBufferFactory.CreateBuffer(queue, settings))
                    .ToList();

            foreach (IMessageReceiveBuffer receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            var consumers = Enumerable.Range(0, settings.ConcurrencyLimit)
                .Select(x => _multiplexerSubscriberFactory.Create())
                .ToList();

            foreach (IMultiplexerSubscriber consumer in consumers)
            {
                consumer.Subscribe(multiplexer.GetMessagesAsync());
            }

            var bus = new SubscriptionGroup(
                settings,
                receiveBuffers,
                multiplexer,
                consumers,
                _loggerFactory.CreateLogger<SubscriptionGroup>());

            return bus;
        }
    }
}
