using System;

namespace JustEat.Simples.NotificationStack.AwsTools.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        void BeforeGettingMoreMessages();
        void ProcessMessage(Action action);
    }
}