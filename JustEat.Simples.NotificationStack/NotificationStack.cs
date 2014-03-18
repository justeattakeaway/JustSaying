using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using NLog;

namespace JustEat.Simples.NotificationStack.Stack
{
    public interface INotificationStack : IMessagePublisher
    {
        bool Listening { get; }
        void AddNotificationTopicSubscriber(string topic, INotificationSubscriber subscriber);
        void AddMessageHandler<T>(string topic, IHandler<T> handler) where T : Message;
        void AddMessagePublisher<T>(string topic, IMessagePublisher messagePublisher) where T : Message;
        void Start();
        void Stop();
        IMessagingConfig Config { get; }
        IMessageMonitor Monitor { get; set; }
        IMessageSerialisationRegister SerialisationRegister { get; }
    }

    public class NotificationStack : INotificationStack
    {
        public bool Listening { get; private set; }

        private readonly Dictionary<string, INotificationSubscriber> _notificationSubscribers;
        private readonly Dictionary<string, Dictionary<Type, IMessagePublisher>> _messagePublishers;
        public IMessagingConfig Config { get; private set; }
        public IMessageMonitor Monitor { get; set; }
        public IMessageSerialisationRegister SerialisationRegister { get; private set; }
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public NotificationStack(IMessagingConfig config, IMessageSerialisationRegister serialisationRegister)
        {
            if (config.PublishFailureReAttempts == 0)
                Log.Warn("You have not set a re-attempt value for publish failures. If the publish location is 'down' you may loose messages!");

            Log.Info(string.Format("Registering with stack - Component: {0}, Tenant: {1}, Environment: {2}", config.Component, config.Tenant, config.Environment));

            Config = config;
            _notificationSubscribers = new Dictionary<string, INotificationSubscriber>();
            _messagePublishers = new Dictionary<string, Dictionary<Type, IMessagePublisher>>();
            SerialisationRegister = serialisationRegister;
        }

        public void AddNotificationTopicSubscriber(string topic, INotificationSubscriber subscriber)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentNullException("topic");

            try
            {
                _notificationSubscribers.Add(topic, subscriber);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("You cannot declare more than one subscriber service for the same topic per component. Topic: {0}", topic.ToString());
            }
        }

        public void AddMessageHandler<T>(string topic, IHandler<T> handler) where T : Message
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentNullException("topic");

            _notificationSubscribers[topic].AddMessageHandler(handler);
        }

        public void AddMessagePublisher<T>(string topic, IMessagePublisher messagePublisher) where T : Message
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentNullException("topic");

            if (! _messagePublishers.ContainsKey(topic))
                _messagePublishers.Add(topic, new Dictionary<Type, IMessagePublisher>());

            _messagePublishers[topic].Add(typeof(T), messagePublisher);
        }

        public void Start()
        {
            if (Listening)
                return;
            
            foreach (var subscription in _notificationSubscribers)
            {
                subscription.Value.Listen();
            }
            Listening = true;
        }

        public void Stop()
        {
            if (!Listening)
                return;

            foreach (var subscription in _notificationSubscribers)
            {
                subscription.Value.StopListening();
            }
            Listening = false;
        }

        public void Publish(Message message)
        {
            message.RaisingComponent = Config.Component;
            message.Tenant = Config.Tenant;

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
                    if (attemptCount == Config.PublishFailureReAttempts)
                    {
                        Monitor.IssuePublishingMessage();
                        Log.ErrorException(string.Format("Unable to publish message {0}", message.GetType().Name), ex);
                        throw;
                    }

                    Thread.Sleep(Config.PublishFailureBackoffMilliseconds * attemptCount); // Increase back off each time
                    Publish(publisher, message, attemptCount);
                }
            };

            publish.BeginInvoke(null, null);
        }
    }
}
