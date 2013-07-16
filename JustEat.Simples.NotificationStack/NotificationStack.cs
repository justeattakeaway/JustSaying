using System;
using System.Collections.Generic;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Stack
{
    public class NotificationStack : IMessagePublisher
    {
        internal Component Component { get; private set; }
        public bool Listening { get; private set; }

        private readonly Dictionary<NotificationTopic, INotificationSubscriber> _notificationSubscribers;
        private readonly Dictionary<NotificationTopic, Dictionary<Type, IMessagePublisher>> _messagePublishers;

        public NotificationStack(Component component)
        {
            Component = component;
            _notificationSubscribers = new Dictionary<NotificationTopic, INotificationSubscriber>();
            _messagePublishers = new Dictionary<NotificationTopic, Dictionary<Type, IMessagePublisher>>();
        }

        public void AddNotificationTopicSubscriber(NotificationTopic topic, INotificationSubscriber subscriber)
        {
            _notificationSubscribers.Add(topic, subscriber);
        }

        public void AddMessageHandler<T>(NotificationTopic topic, IHandler<T> handler) where T : Message
        {
            _notificationSubscribers[topic].AddMessageHandler(handler);
        }

        public void AddMessagePublisher<T>(NotificationTopic topic, IMessagePublisher messagePublisher) where T : Message
        {
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
            var published = false;
            foreach (var topicPublisher in _messagePublishers.Values)
            {
                if (!topicPublisher.ContainsKey(message.GetType()))
                    continue;

                topicPublisher[message.GetType()].Publish(message);
                published = true;
            }

            if (!published)
                throw new InvalidOperationException("This message is not registered for publication");
        }
    }
}
