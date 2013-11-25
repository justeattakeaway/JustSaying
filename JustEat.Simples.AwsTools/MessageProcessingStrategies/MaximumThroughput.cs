using System;

namespace JustEat.Simples.NotificationStack.AwsTools.MessageProcessingStrategies.JustEat.Simples.NotificationStack.AwsTools.MessageProcessingStrategies
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