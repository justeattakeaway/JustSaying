using System;

namespace JustEat.Simples.NotificationStack.Messaging.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        void BeforeGettingMoreMessages();
        void ProcessMessage(Action action);
    }
}