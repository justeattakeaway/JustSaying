using System.Collections.Generic;
using SimplesNotificationStack.Messaging;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Stack
{
    public class NotificationStack
    {
        private readonly List<KeyValuePair<NotificationTopic, IMessageSubscriber>> _notificationSubscribers;

        public NotificationStack(Messaging.Component component)
        {
            _notificationSubscribers = new List<KeyValuePair<NotificationTopic, IMessageSubscriber>>();
        }

        public void AddNotificationTopicSubscriber(NotificationTopic topic, IMessageSubscriber subscriber)
        {
            _notificationSubscribers.Add(new KeyValuePair<NotificationTopic, IMessageSubscriber>(topic, subscriber));
        }

        public void AddMessageHandler(NotificationTopic topic, IHandler<Message> handler)
        {

        }

        public void Start()
        {
            _notificationSubscribers.ForEach(x => x.Value.Listen());
        }
    }
}
