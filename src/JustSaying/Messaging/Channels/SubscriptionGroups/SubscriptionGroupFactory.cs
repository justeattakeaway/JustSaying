using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal class SubscriptionGroupFactory : ISubscriptionGroupFactory
    {
        private readonly SubscriptionConfig _subscriptionConfig;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IMessageMonitor _monitor;
        private readonly ILoggerFactory _loggerFactory;

        public SubscriptionGroupFactory(
            SubscriptionConfig subscriptionConfig,
            IMessageDispatcher messageDispatcher,
            IMessageMonitor monitor,
            ILoggerFactory loggerFactory)
        {
            _subscriptionConfig = subscriptionConfig;
            _messageDispatcher = messageDispatcher;
            _monitor = monitor;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public SubscriptionGroupCollection Create(
            IDictionary<string, SubscriptionGroupSettingsBuilder> consumerGroupSettings)
        {
            List<ISubscriptionGroup> buses = consumerGroupSettings
                .Values
                .Select(builder => Create(builder.Build()))
                .ToList();

            return new SubscriptionGroupCollection(
                buses,
                _loggerFactory.CreateLogger<SubscriptionGroupCollection>());
        }

        private ISubscriptionGroup Create(SubscriptionGroupSettings settings)
        {
            IMultiplexer multiplexer = CreateMultiplexer(settings.MultiplexerCapacity);
            ICollection<IMessageReceiveBuffer> receiveBuffers = CreateBuffers(settings);
            ICollection<IMultiplexerSubscriber> subscribers = CreateSubscribers(settings.ConcurrencyLimit);

            foreach (IMessageReceiveBuffer receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            foreach (IMultiplexerSubscriber consumer in subscribers)
            {
                consumer.Subscribe(multiplexer.GetMessagesAsync());
            }

            return new SubscriptionGroup(
                settings,
                receiveBuffers,
                multiplexer,
                subscribers,
                _loggerFactory.CreateLogger<SubscriptionGroup>());
        }

        private ICollection<IMessageReceiveBuffer> CreateBuffers(
            SubscriptionGroupSettings subscriptionGroupSettings)
        {
            var buffers = new List<IMessageReceiveBuffer>();

            foreach (ISqsQueue queue in subscriptionGroupSettings.Queues)
            {
                var buffer = new MessageReceiveBuffer(
                    subscriptionGroupSettings.Prefetch,
                    subscriptionGroupSettings.BufferSize,
                    subscriptionGroupSettings.ReceiveBufferReadTimeout,
                    subscriptionGroupSettings.ReceiveBufferWriteTimeout,
                    queue,
                    _subscriptionConfig.SqsMiddleware,
                    _monitor,
                    _loggerFactory.CreateLogger<MessageReceiveBuffer>());

                buffers.Add(buffer);
            }

            return buffers;
        }

        private IMultiplexer CreateMultiplexer(int channelCapacity)
        {
            return new RoundRobinQueueMultiplexer(
                channelCapacity,
                _loggerFactory.CreateLogger<RoundRobinQueueMultiplexer>());
        }

        private ICollection<IMultiplexerSubscriber> CreateSubscribers(int count)
        {
            return Enumerable.Range(0, count)
                .Select(x => (IMultiplexerSubscriber)new MultiplexerSubscriber(_messageDispatcher))
                .ToList();
        }
    }
}
