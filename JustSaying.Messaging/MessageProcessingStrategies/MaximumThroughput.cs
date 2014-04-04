using System;

namespace JustSaying.Messaging.MessageProcessingStrategies
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