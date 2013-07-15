using System;
using System.Collections.Generic;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Stack
{
    public class NotificationStack
    {
        internal Component Component { get; private set; }
        public bool Listening { get; private set; }

        private readonly Dictionary<NotificationTopic, INotificationSubscriber> _notificationSubscribers;

        public NotificationStack(Component component)
        {
            Component = component;
            _notificationSubscribers = new Dictionary<NotificationTopic, INotificationSubscriber>();
        }

        public void AddNotificationTopicSubscriber(NotificationTopic topic, INotificationSubscriber subscriber)
        {
            _notificationSubscribers.Add(topic, subscriber);
        }

        public void AddMessageHandler<T>(NotificationTopic topic, IHandler<T> handler) where T : Message
        {
            _notificationSubscribers[topic].AddMessageHandler(handler);
        }

        public void Start()
        {
            foreach (var subscription in _notificationSubscribers)
            {
                subscription.Value.Listen();
            }
            Listening = true;
        }

        public void Stop()
        {
            foreach (var subscription in _notificationSubscribers)
            {
                subscription.Value.StopListening();
            }
            Listening = false;
        }
    }
}
