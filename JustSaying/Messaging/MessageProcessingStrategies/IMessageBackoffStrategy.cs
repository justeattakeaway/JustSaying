using JustSaying.Models;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageBackoffStrategy
    {
        int GetVisibilityTimeout(Message message, int approximateReceiveCount);
    }
}