using System;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public class DefaultThrottledThroughput : Throttled
    {
        public DefaultThrottledThroughput(IMessageMonitor messageMonitor) :
            base(MaxActiveHandlersForProcessors(), MessageConstants.MaxAmazonMessageCap, messageMonitor)
        {

        }

        private static int MaxActiveHandlersForProcessors()
        {
            return Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;
        }
    }
}
