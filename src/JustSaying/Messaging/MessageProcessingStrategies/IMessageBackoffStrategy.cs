using JustSaying.Models;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageBackoffStrategy
    {
        TimeSpan GetBackoffDuration(Message message, int approximateReceiveCount, Exception lastException = null);
    }
}