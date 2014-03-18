using System;

namespace JustEat.Simples.NotificationStack.Messaging.MessageProcessingStrategies
{
    public class MaximumThroughput : IMessageProcessingStrategy
    {
        public void BeforeGettingMoreMessages()
        {
        }

        public void ProcessMessage(Action action)
        {
            action.BeginInvoke(null, null);
        }
    }
}