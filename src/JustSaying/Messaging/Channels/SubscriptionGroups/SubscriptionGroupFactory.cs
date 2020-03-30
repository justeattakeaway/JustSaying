using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Configuration;
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
                .Select(Create)
                .ToList();

            return new SubscriptionGroupCollection(
                buses,
                _loggerFactory.CreateLogger<SubscriptionGroupCollection>());
        }

        public ISubscriptionGroup Create(SubscriptionGroupSettingsBuilder settingsBuilder)
        {
            SubscriptionGroupSettings settings = settingsBuilder.Build();

            IReadOnlyCollection<ISqsQueue> groupQueues = settings.Queues;

            IMultiplexer multiplexer = CreateMultiplexer(settings.MultiplexerCapacity);

            var receiveBuffers = groupQueues
                    .Select(queue => CreateBuffer(queue, settings))
                    .ToList();

            foreach (IMessageReceiveBuffer receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            var subscribers = Enumerable.Range(0, settings.ConcurrencyLimit)
                .Select(x => CreateSubscriber())
                .ToList();

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

        private IMessageReceiveBuffer CreateBuffer(
            ISqsQueue queue,
            SubscriptionGroupSettings subscriptionGroupSettings)
        {
            var buffer = new MessageReceiveBuffer(
                subscriptionGroupSettings.BufferSize,
                queue,
                _subscriptionConfig.SqsMiddleware,
                _monitor,
                _loggerFactory.CreateLogger<MessageReceiveBuffer>());

            return buffer;
        }

        private IMultiplexer CreateMultiplexer(int channelCapacity)
        {
            return new RoundRobinQueueMultiplexer(
                channelCapacity,
                _loggerFactory.CreateLogger<RoundRobinQueueMultiplexer>());
        }

        private IMultiplexerSubscriber CreateSubscriber()
        {
            return new MultiplexerSubscriber(_messageDispatcher);
        }
    }
}
