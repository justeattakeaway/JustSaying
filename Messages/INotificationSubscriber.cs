using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging
{
    public interface INotificationSubscriber
    {
        void AddMessageHandler(IHandler<Message> handler);
        void Listen();
    }
}