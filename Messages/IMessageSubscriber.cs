using System.Collections.Generic;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging
{
    public interface IMessageSubscriber
    {
        void AddMessageHandler(IHandler<Message> handlers);
        void Listen();
    }
}