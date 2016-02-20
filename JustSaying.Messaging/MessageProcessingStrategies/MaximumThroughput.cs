using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public class MaximumThroughput : IMessageProcessingStrategy
    {
        public Task BeforeGettingMoreMessages()
        {
            return Task.FromResult(true);
        }

        public void ProcessMessage(Action action)
        {
            action.BeginInvoke(null, null);
        }
    }
}