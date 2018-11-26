using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public sealed class JustSayingBus : IAmJustSaying, IAmJustInterrogating
    {
        private readonly Dictionary<string, Dictionary<string, INotificationSubscriber>> _subscribersByRegionAndQueue;
        private readonly Dictionary<string, Dictionary<string, IMessagePublisher>> _publishersByRegionAndTopic;
        public IMessagingConfig Config { get; private set; }

        private IMessageMonitor _monitor;
        public IMessageMonitor Monitor
        {
            get { return _monitor; }
            set { _monitor = value ?? new NullOpMessageMonitor(); }
        }
        public IMessageSerializationRegister SerializationRegister { get; private set; }
        public IMessageLockAsync MessageLock { get; set; }

        private ILogger _log;

        private readonly object _syncRoot = new object();
        private readonly ICollection<IPublisher> _publishers;
        private readonly ICollection<ISubscriber> _subscribers;

        public JustSayingBus(IMessagingConfig config, IMessageSerializationRegister serializationRegister, ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger("JustSaying");

            Config = config;
            Monitor = new NullOpMessageMonitor();

            _subscribersByRegionAndQueue = new Dictionary<string, Dictionary<string, INotificationSubscriber>>();
            _publishersByRegionAndTopic = new Dictionary<string, Dictionary<string, IMessagePublisher>>();
            SerializationRegister = serializationRegister;
            _publishers = new HashSet<IPublisher>();
            _subscribers = new HashSet<ISubscriber>();
        }

        public void AddNotificationSubscriber(string region, INotificationSubscriber subscriber)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (!_subscribersByRegionAndQueue.TryGetValue(region, out var subscribersForRegion))
            {
                subscribersForRegion = new Dictionary<string,INotificationSubscriber>();
                _subscribersByRegionAndQueue.Add(region, subscribersForRegion);
            }

            if (subscribersForRegion.ContainsKey(subscriber.Queue))
            {
                // TODO - no, we don't need to create a new notification subsrciber per queue
                // JustSaying is creating subscribers per-topic per-region, but
                // we want to have that per-queue per-region, not
                // per-topic per-region.
                // Just re-use existing subscriber instead.
                return;
            }
            subscribersForRegion[subscriber.Queue] = subscriber;

            AddSubscribersToInterrogationResponse(subscriber);
        }

        private void AddSubscribersToInterrogationResponse(INotificationSubscriberInterrogation interrogationSubscribers)
        {
            foreach (var subscriber in interrogationSubscribers.Subscribers)
            {
                _subscribers.Add(subscriber);
            }
        }

        public void AddMessageHandler<T>(string region, string queue, Func<IHandlerAsync<T>> futureHandler) where T : Message
        {
            var subscribersByRegion = _subscribersByRegionAndQueue[region];
            var subscriber = subscribersByRegion[queue];
            subscriber.AddMessageHandler(futureHandler);
        }

        public void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message
        {
            if (Config.PublishFailureReAttempts == 0)
            {
                _log.LogWarning("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may lose messages!");
            }

            if (!_publishersByRegionAndTopic.TryGetValue(region, out var publishersByTopic))
            {
                publishersByTopic = new Dictionary<string, IMessagePublisher>();
                _publishersByRegionAndTopic.Add(region, publishersByTopic);
            }

            var topic = typeof(T).ToTopicName();
            _publishers.Add(new Publisher(typeof(T)));

            publishersByTopic[topic] = messagePublisher;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            lock (_syncRoot)
            {
                foreach (var regionSubscriber in _subscribersByRegionAndQueue)
                {
                    foreach (var queueSubscriber in regionSubscriber.Value)
                    {
                        if (queueSubscriber.Value.IsListening)
                        {
                            continue;
                        }

                        queueSubscriber.Value.Listen(cancellationToken);
                    }
                }
            }
        }

        public async Task PublishAsync(PublishEnvelope envelope, CancellationToken cancellationToken)
        {
            var publisher = GetActivePublisherForMessage(envelope.Message);
            await PublishAsync(publisher, envelope, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public IInterrogationResponse WhatDoIHave()
        {
            return new InterrogationResponse(Config.Regions, _subscribers, _publishers);
        }

        private IMessagePublisher GetActivePublisherForMessage(Message message)
        {
            string activeRegion;
            if (Config.GetActiveRegion == null)
            {
                activeRegion = Config.Regions.First();
            }
            else
            {
                activeRegion = Config.GetActiveRegion();
            }
            _log.LogInformation($"Active region has been evaluated to {activeRegion}");

            if (!_publishersByRegionAndTopic.ContainsKey(activeRegion))
            {
                var errorMessage = $"Error publishing message, no publishers registered for region {activeRegion}.";
                _log.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var topic = message.GetType().ToTopicName();
            var publishersByTopic = _publishersByRegionAndTopic[activeRegion];
            if (!publishersByTopic.ContainsKey(topic))
            {
                var errorMessage = $"Error publishing message, no publishers registered for message type {message} in {activeRegion}.";
                _log.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return publishersByTopic[topic];
        }

        private async Task PublishAsync(
            IMessagePublisher publisher,
            PublishEnvelope envelope,
            int attemptCount = 0,
            CancellationToken cancellationToken = default)
        {
            attemptCount++;
            try
            {
                var watch = Stopwatch.StartNew();

                await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                watch.Stop();
                Monitor.PublishMessageTime(watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (attemptCount >= Config.PublishFailureReAttempts)
                {
                    Monitor.IssuePublishingMessage();
                    _log.LogError(0, ex, $"Failed to publish message {envelope.Message.GetType()}. Halting after attempt {attemptCount}");
                    throw;
                }

                _log.LogWarning(0, ex, $"Failed to publish message {envelope.Message.GetType()}. Retrying after attempt {attemptCount} of {Config.PublishFailureReAttempts}");

                var delayForAttempt = TimeSpan.FromMilliseconds(Config.PublishFailureBackoff.TotalMilliseconds * attemptCount);
                await Task.Delay(delayForAttempt, cancellationToken).ConfigureAwait(false);

                await PublishAsync(publisher, envelope, attemptCount, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
