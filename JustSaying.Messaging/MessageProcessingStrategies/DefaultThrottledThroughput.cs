using System;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public class DefaultThrottledThroughput : Throttled
    {
        private const int MaxAmazonMessageCap = 10;
        private static int ParallelHandlerExecutionPerCore = 8;

        public DefaultThrottledThroughput(IMessageMonitor messageMonitor) :
            base(ParallelHandlerExecutionPerCore * Environment.ProcessorCount, MaxAmazonMessageCap, messageMonitor)
        {

        }
    }
}
