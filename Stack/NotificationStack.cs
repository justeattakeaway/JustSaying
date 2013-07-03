using System.Collections.Generic;
using SimplesNotificationStack.Messaging;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Stack
{
    public class NotificationStack
    {
        private readonly Dictionary<NotificationTopic, IMessageSubscriber> _notificationSubscribers;

        public NotificationStack(Component component)
        {
            _notificationSubscribers = new Dictionary<NotificationTopic, IMessageSubscriber>();
        }

        public void AddNotificationTopicSubscriber(NotificationTopic topic, IMessageSubscriber subscriber)
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
