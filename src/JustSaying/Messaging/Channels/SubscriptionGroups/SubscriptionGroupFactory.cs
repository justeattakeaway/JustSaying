using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using ReceiveMiddleware =
    JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Channels.Context.GetMessagesContext,
        System.Collections.Generic.IList<Amazon.SQS.Model.Message>>;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <summary>
    /// Builds <see cref="ISubscriptionGroup"/>'s from the various components required.
    /// </summary>
    public class SubscriptionGroupFactory : ISubscriptionGroupFactory
    {
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IMessageMonitor _monitor;
        private readonly ILoggerFactory _loggerFactory;
        private ReceiveMiddleware _defaultSqsMiddleware;

        /// <summary>
        /// Creates an instance of <see cref="SubscriptionGroupFactory"/>.
        /// </summary>
        /// <param name="messageDispatcher">The <see cref="IMessageDispatcher"/> to use to dispatch messages.</param>
        /// <param name="monitor">The <see cref="IMessageMonitor"/> used by the <see cref="IMessageReceiveBuffer"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
        public SubscriptionGroupFactory(
            IMessageDispatcher messageDispatcher,
            IMessageMonitor monitor,
            ILoggerFactory loggerFactory)
        {
            _messageDispatcher = messageDispatcher;
            _monitor = monitor;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _defaultSqsMiddleware =
                new DefaultSqsMiddleware(_loggerFactory.CreateLogger<DefaultSqsMiddleware>());
        }

        /// <summary>
        /// Creates a <see cref="ISubscriptionGroup"/> for the given configuration.
        /// </summary>
        /// <param name="defaults">The default values to use while building each <see cref="SubscriptionGroup"/>.</param>
        /// <param name="subscriptionGroupSettings"></param>
        /// <returns>An <see cref="ISubscriptionGroup"/> to run.</returns>
        public ISubscriptionGroup Create(
            SubscriptionGroupSettingsBuilder defaults,
            IDictionary<string, SubscriptionGroupConfigBuilder> subscriptionGroupSettings)
        {
            ReceiveMiddleware receiveMiddleware = defaults.SqsMiddleware ?? _defaultSqsMiddleware;

            List<ISubscriptionGroup> groups = subscriptionGroupSettings
                .Values
                .Select(builder => Create(receiveMiddleware, builder.Build(defaults)))
                .ToList();

            return new SubscriptionGroupCollection(
                groups,
                _loggerFactory.CreateLogger<SubscriptionGroupCollection>());
        }

        private ISubscriptionGroup Create(ReceiveMiddleware receiveMiddleware, SubscriptionGroupSettings settings)
        {
            IMultiplexer multiplexer = CreateMultiplexer(settings.MultiplexerCapacity);
            ICollection<IMessageReceiveBuffer> receiveBuffers = CreateBuffers(receiveMiddleware, settings);
            ICollection<IMultiplexerSubscriber> subscribers = CreateSubscribers(settings);

            foreach (IMessageReceiveBuffer receiveBuffer in receiveBuffers)
            {
                multiplexer.ReadFrom(receiveBuffer.Reader);
            }

            foreach (IMultiplexerSubscriber subscriber in subscribers)
            {
                subscriber.Subscribe(multiplexer.GetMessagesAsync());
            }

            return new SubscriptionGroup(
                settings,
                receiveBuffers,
                multiplexer,
                subscribers,
                _loggerFactory.CreateLogger<SubscriptionGroup>());
        }

        private ICollection<IMessageReceiveBuffer> CreateBuffers(
            ReceiveMiddleware receiveMiddleware,
            SubscriptionGroupSettings subscriptionGroupSettings)
        {
            var buffers = new List<IMessageReceiveBuffer>();

            foreach (ISqsQueue queue in subscriptionGroupSettings.Queues)
            {
                var buffer = new MessageReceiveBuffer(
                    subscriptionGroupSettings.Prefetch,
                    subscriptionGroupSettings.BufferSize,
                    subscriptionGroupSettings.ReceiveBufferReadTimeout,
                    subscriptionGroupSettings.ReceiveMessagesWaitTime,
                    queue,
                    receiveMiddleware,
                    _monitor,
                    _loggerFactory.CreateLogger<MessageReceiveBuffer>());

                buffers.Add(buffer);
            }

            return buffers;
        }

        private IMultiplexer CreateMultiplexer(int channelCapacity)
        {
            return new MergingMultiplexer(
                channelCapacity,
                _loggerFactory.CreateLogger<MergingMultiplexer>());
        }

        private ICollection<IMultiplexerSubscriber> CreateSubscribers(SubscriptionGroupSettings settings)
        {
            var logger = _loggerFactory.CreateLogger<MultiplexerSubscriber>();

            return Enumerable.Range(0, settings.ConcurrencyLimit)
                .Select(num => (IMultiplexerSubscriber) new MultiplexerSubscriber(
                    _messageDispatcher,
                    $"{settings.Name}-subscriber-{num}",
                    logger))
                .ToList();
        }
    }
}
