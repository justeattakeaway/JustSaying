using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging
{
    public interface INotificationSubscriber
    {
        void AddMessageHandler(IHandler<Message> handler);
        void Listen();
    }
}