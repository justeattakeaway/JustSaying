using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging
{
    public interface IMessagePublisher
    {
        void Publish(Message message);
    }
}
