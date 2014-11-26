using System;
using System.Collections.Generic;
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

        private readonly Dictionary<string, IList<INotificationSubscriber>> _notificationSubscribers;
        private readonly Dictionary<string, Dictionary<Type, IMessagePublisher>> _messagePublishers;
        public IMessagingConfig Config { get; private set; }

        private IMessageMonitor _monitor;
        public IMessageMonitor Monitor { 
            get { return _monitor;  }
            set { _monitor = value ?? new NullOpMessageMonitor(); }
        }
        public IMessageSerialisationRegister SerialisationRegister { get; private set; }
        public IMessageLock MessageLock { get; set; }
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); //ToDo: danger!

        public JustSayingBus(IMessagingConfig config, IMessageSerialisationRegister serialisationRegister)
        {
            if (config.PublishFailureReAttempts == 0)
                Log.Warn("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may loose messages!");

            Log.Info(string.Format("Registering with stack."));

            Config = config;
            Monitor = new NullOpMessageMonitor();

            _notificationSubscribers = new Dictionary<string, IList<INotificationSubscriber>>();
            _messagePublishers = new Dictionary<string, Dictionary<Type, IMessagePublisher>>();
            SerialisationRegister = serialisationRegister;
        }

        public void AddNotificationTopicSubscriber(string topic, INotificationSubscriber subscriber)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentNullException("topic");

            IList<INotificationSubscriber> subscribersForTopic;
            if (!_notificationSubscribers.TryGetValue(topic, out subscribersForTopic))
            {
                subscribersForTopic = new List<INotificationSubscriber>();
                _notificationSubscribers.Add(topic, subscribersForTopic);
            }

            subscribersForTopic.Add(subscriber);
        }

        public void AddMessageHandler<T>(IHandler<T> handler) where T : Message
        {
            var topic = typeof(T).Name.ToLower();

            foreach (var subscriber in _notificationSubscribers[topic])
            {
                subscriber.AddMessageHandler(handler);                
            }
        }

        public void AddMessagePublisher<T>(IMessagePublisher messagePublisher) where T : Message
        {
            var topic = typeof (T).Name.ToLower();

            if (! _messagePublishers.ContainsKey(topic))
                _messagePublishers.Add(topic, new Dictionary<Type, IMessagePublisher>());

            _messagePublishers[topic].Add(typeof(T), messagePublisher);
        }

        public void Start()
        {
            if (Listening)
                return;
            
            foreach (var subscriptions in _notificationSubscribers)
            {
                foreach (var subscriber in subscriptions.Value)
                {
                    subscriber.Listen();
                }
            }

            Listening = true;
        }

        public void Stop()
        {
            if (!Listening)
                return;

            foreach (var subscribers in _notificationSubscribers)
            {
                foreach (var subscriber in subscribers.Value)
                {
                    subscriber.StopListening();
                }
            }
            Listening = false;
        }

        public void Publish(Message message)
        {
            var published = false;
            foreach (var topicPublisher in _messagePublishers.Values)
            {
                if (!topicPublisher.ContainsKey(message.GetType()))
                    continue;

                Publish(topicPublisher[message.GetType()], message);
                published = true;
            }

            if (!published)
            {
                Log.Error("Error publishing message, no publisher registered for message type: {0}.", message.ToString());
                throw new InvalidOperationException(string.Format("This message is not registered for publication: '{0}'", message));
            }
        }

        private void Publish(IMessagePublisher publisher, Message message, int attemptCount = 0)
        {
            Action publish = () =>
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
                    if(Monitor == null)
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
            };

            publish.BeginInvoke(null, null);
        }
    }
}