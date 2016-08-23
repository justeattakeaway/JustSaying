using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using NLog;

namespace JustSaying
{
    public class JustSayingBus : IAmJustSaying, IAmJustInterrogating
    {
        public bool Listening { get; private set; }

        private readonly Dictionary<string, Dictionary<string, INotificationSubscriber>> _subscribersByRegionAndQueue;
        private readonly Dictionary<string, Dictionary<string, IMessagePublisher>> _publishersByRegionAndTopic;
        public IMessagingConfig Config { get; private set; }

        private IMessageMonitor _monitor;
        public IMessageMonitor Monitor
        {
            get { return _monitor; }
            set { _monitor = value ?? new NullOpMessageMonitor(); }
        }
        public IMessageSerialisationRegister SerialisationRegister { get; private set; }
        public IMessageLock MessageLock { get; set; }
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); //ToDo: danger!
        private readonly object _syncRoot = new object();
        private readonly ICollection<IPublisher> _publishers;
        private readonly ICollection<ISubscriber> _subscribers;

        public JustSayingBus(IMessagingConfig config, IMessageSerialisationRegister serialisationRegister)
        {
            if (config.PublishFailureReAttempts == 0)
            {
                Log.Warn("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may lose messages!");
            }

            Log.Info("Registering with stack.");

            Config = config;
            Monitor = new NullOpMessageMonitor();

            _subscribersByRegionAndQueue = new Dictionary<string, Dictionary<string, INotificationSubscriber>>();
            _publishersByRegionAndTopic = new Dictionary<string, Dictionary<string, IMessagePublisher>>();
            SerialisationRegister = serialisationRegister;
            _publishers = new HashSet<IPublisher>();
            _subscribers = new HashSet<ISubscriber>();
        }

        public void AddNotificationSubscriber(string region, INotificationSubscriber subscriber)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException(nameof(region));
            }

            Dictionary<string, INotificationSubscriber> subscribersForRegion;
            if (!_subscribersByRegionAndQueue.TryGetValue(region, out subscribersForRegion))
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
            Dictionary<string, IMessagePublisher> publishersByTopic;
            if (!_publishersByRegionAndTopic.TryGetValue(region, out publishersByTopic))
            {
                publishersByTopic = new Dictionary<string, IMessagePublisher>();
                _publishersByRegionAndTopic.Add(region, publishersByTopic);
            }

            var topic = typeof(T).ToTopicName();
            _publishers.Add(new Publisher(typeof(T)));

            publishersByTopic[topic] = messagePublisher;
        }

        public void Start()
        {
            lock (_syncRoot)
            {
                if (Listening)
                    return;
                foreach (var regionSubscriber in _subscribersByRegionAndQueue)
                {
                    foreach (var queueSubscriber in regionSubscriber.Value)
                    {
                        queueSubscriber.Value.Listen();
                    }
                }

                Listening = true;
            }
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                if (!Listening)
                    return;
                foreach (var regionSubscriber in _subscribersByRegionAndQueue)
                {
                    foreach (var queueSubscriber in regionSubscriber.Value)
                    {
                        queueSubscriber.Value.StopListening();
                    }
                }
                Listening = false;
            }
        }

        public async Task Publish(Message message)
        {
            var publisher = GetActivePublisherForMessage(message);
            await Publish(publisher, message);
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
            Log.Info($"Active region has been evaluated to {activeRegion}");

            if (!_publishersByRegionAndTopic.ContainsKey(activeRegion))
            {
                var errorMessage = $"Error publishing message, no publishers registered for region {activeRegion}.";
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var topic = message.GetType().ToTopicName();
            var publishersByTopic = _publishersByRegionAndTopic[activeRegion];
            if (!publishersByTopic.ContainsKey(topic))
            {
                var errorMessage = $"Error publishing message, no publishers registered for message type {message} in {activeRegion}.";
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return publishersByTopic[topic];
        }

        private async Task Publish(IMessagePublisher publisher, Message message, int attemptCount = 0)
        {
            attemptCount++;
            try
            {
                var watch = Stopwatch.StartNew();

                await publisher.Publish(message);

                watch.Stop();
                Monitor.PublishMessageTime(watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (attemptCount >= Config.PublishFailureReAttempts)
                {
                    Monitor.IssuePublishingMessage();
                    Log.Error(ex, $"Unable to publish message {message.GetType().Name} after {attemptCount} attempts");
                    throw;
                }

                await Task.Delay(Config.PublishFailureBackoffMilliseconds*attemptCount);
                await Publish(publisher, message, attemptCount);
            }
      
        }
        public IInterrogationResponse WhatDoIHave()
        {
            return new InterrogationResponse(Config.Regions, _subscribers, _publishers);
        }
    }
}