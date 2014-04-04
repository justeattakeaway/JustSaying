using JustSaying.Messaging.Messages;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
        void Publish(Message message);
    }
}
