using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging
{
    public interface IMessagePublisher
    {
        void Publish(Message message);
    }
}
