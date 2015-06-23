using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using NLog;

namespace JustSaying
{
    public class JustSayingBus : IAmJustSaying
    {
        public bool Listening { get; private set; }

        private readonly Dictionary<string, IList<INotificationSubscriber>> _subscribersByTopic;
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

        public JustSayingBus(IMessagingConfig config, IMessageSerialisationRegister serialisationRegister)
        {
            if (config.PublishFailureReAttempts == 0)
                Log.Warn("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may loose messages!");

            Log.Info(string.Format("Registering with stack."));

            Config = config;
            
            Monitor = new NullOpMessageMonitor();

            _subscribersByTopic = new Dictionary<string, IList<INotificationSubscriber>>();
            _publishersByRegionAndTopic = new Dictionary<string, Dictionary<string, IMessagePublisher>>();
            SerialisationRegister = serialisationRegister;
        }

        public void AddNotificationTopicSubscriber(string topic, INotificationSubscriber subscriber)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentNullException("topic");

            IList<INotificationSubscriber> subscribersForTopic;
            if (!_subscribersByTopic.TryGetValue(topic, out subscribersForTopic))
            {
                subscribersForTopic = new List<INotificationSubscriber>();
                _subscribersByTopic.Add(topic, subscribersForTopic);
            }

            subscribersForTopic.Add(subscriber);
        }

        public void AddMessageHandler<T>(Func<IHandler<T>> futureHandler) where T : Message
        {
            var topic = Config.TopicNameProvider(typeof(T));

            foreach (var subscriber in _subscribersByTopic[topic])
            {
                subscriber.AddMessageHandler(futureHandler);
            }
        }

        public void AddMessagePublisher<T>(IMessagePublisher messagePublisher, string region) where T : Message
        {
            Dictionary<string, IMessagePublisher> publishersByTopic;
            if (!_publishersByRegionAndTopic.TryGetValue(region, out publishersByTopic))
            {
                publishersByTopic = new Dictionary<string, IMessagePublisher>();
                _publishersByRegionAndTopic.Add(region, publishersByTopic);
            }

            var topic = Config.TopicNameProvider(typeof(T));
            publishersByTopic[topic] = messagePublisher;
        }

        public void Start()
        {
            lock (_syncRoot)
            {
                if (!Listening)
                {
                    foreach (var subscriptions in _subscribersByTopic)
                    {
                        foreach (var subscriber in subscriptions.Value)
                        {
                            subscriber.Listen();
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
                    foreach (var subscribers in _subscribersByTopic)
                    {
                        foreach (var subscriber in subscribers.Value)
                        {
                            subscriber.StopListening();
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

            var topic = Config.TopicNameProvider(message.GetType());
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
                var watch = new Stopwatch();
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

                Thread.Sleep(Config.PublishFailureBackoffMilliseconds * attemptCount); // ToDo: Increase back off each time (exponential)
                Publish(publisher, message, attemptCount);
            }
        }
    }
}