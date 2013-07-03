using System.Collections.Generic;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Stack
{
    public class NotificationStack
    {
        internal Component Component { get; private set; }

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

        public void AddMessageHandler(NotificationTopic topic, IHandler<Message> handler)
        {
            _notificationSubscribers[topic].AddMessageHandler(handler);
        }

        public void Start()
        {
            foreach (var subscription in _notificationSubscribers)
            {
                subscription.Value.Listen();
            }
        }
    }
}
