using System;
using System.Threading;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public class DefaultThrottledThroughput : Throttled
    {
        private static readonly int MaxActiveHandlersForProcessors =
            Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;

        public DefaultThrottledThroughput(IMessageMonitor messageMonitor, ILogger logger)
            : base(MaxActiveHandlersForProcessors, Timeout.InfiniteTimeSpan, messageMonitor, logger)
        {
        }
    }
}
