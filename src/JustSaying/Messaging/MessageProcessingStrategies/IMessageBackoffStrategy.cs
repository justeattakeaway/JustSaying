using System;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageBackoffStrategy
    {
        TimeSpan GetBackoffDuration(object message, int approximateReceiveCount, Exception lastException = null);
    }
}
