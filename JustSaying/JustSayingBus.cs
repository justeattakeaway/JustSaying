using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private readonly IList<IPublisher> _publishers;
        private readonly IList<ISubscriber> _subscribers;

        public JustSayingBus(IMessagingConfig config, IMessageSerialisationRegister serialisationRegister)
        {
            if (config.PublishFailureReAttempts == 0)
                Log.Warn("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may loose messages!");

            Log.Info(string.Format("Registering with stack."));

            Config = config;
            Monitor = new NullOpMessageMonitor();

            _subscribersByRegionAndQueue = new Dictionary<string, Dictionary<string, INotificationSubscriber>>();
            _publishersByRegionAndTopic = new Dictionary<string, Dictionary<string, IMessagePublisher>>();
            SerialisationRegister = serialisationRegister;
            _publishers = new List<IPublisher>();
            _subscribers = new List<ISubscriber>();
            _subscribers = new List<ISubscriber>();
        }

        public void AddNotificationSubscriber(string region, INotificationSubscriber subscriber)
        {
            if (string.IsNullOrWhiteSpace(region))
                throw new ArgumentNullException("region");

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

        public void AddMessageHandler<T>(string region, string queue, Func<IHandler<T>> futureHandler) where T : Message
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
                if (!Listening)
                {
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
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                if (Listening)
                {
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
        }

        public void Publish(Message message)
        {
            var publisher = GetActivePublisherForMessage(message);
            Publish(publisher, message);
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
            Log.Info("Active region has been evaluated to {0}", activeRegion);

            if (!_publishersByRegionAndTopic.ContainsKey(activeRegion))
            {
                var errorMessage = string.Format("Error publishing message, no publishers registered for region {0}.", activeRegion);
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var topic = message.GetType().ToTopicName();
            var publishersByTopic = _publishersByRegionAndTopic[activeRegion];
            if (!publishersByTopic.ContainsKey(topic))
            {
                var errorMessage = string.Format("Error publishing message, no publishers registered for message type {0} in {1}.", message, activeRegion);
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return publishersByTopic[topic];
        }

        private void Publish(IMessagePublisher publisher, Message message, int attemptCount = 0)
        {
            attemptCount++;
            try
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                publisher.Publish(message);

                watch.Stop();
                Monitor.PublishMessageTime(watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (Monitor == null)
                    Log.Error("Publish: Monitor was null - duplicates will occur!");

                if (attemptCount == Config.PublishFailureReAttempts)
                {
                    Monitor.IssuePublishingMessage();

                    Log.ErrorException(string.Format("Unable to publish message {0}", message.GetType().Name), ex);
                    throw;
                }

                Thread.Sleep(Config.PublishFailureBackoffMilliseconds * attemptCount); // ToDo: Increase back off each time (linear)
                Publish(publisher, message, attemptCount);
            }
      
        }
        public IInterrogationResponse WhatDoIHave()
        {
            return new InterrogationResponse(_subscribers, _publishers);
        }
    }
}