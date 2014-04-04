using System;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        void BeforeGettingMoreMessages();
        void ProcessMessage(Action action);
    }
}