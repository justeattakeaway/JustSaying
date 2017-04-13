using System;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageBackoffStrategy
    {
        TimeSpan GetVisibilityTimeout(Message message, int approximateReceiveCount);
    }
}