using System;
using System.Threading;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    /// <summary>
    /// A class representing the default implementation of <see cref="Throttled"/>.
    /// </summary>
    public class DefaultThrottledThroughput : Throttled
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultThrottledThroughput"/> class.
        /// </summary>
        /// <param name="options">The <see cref="ThrottledOptions"/> to use.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> does not specify an <see cref="ILogger"/> or <see cref="IMessageMonitor"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The concurrency specified by <paramref name="options"/> is less than one,
        /// or the start timeout specified by <paramref name="options"/> is zero or negative.
        /// </exception>
        public DefaultThrottledThroughput(IMessageMonitor messageMonitor, ILogger logger)
            : base(CreateOptions(messageMonitor, logger))
        {
        }

        private static ThrottledOptions CreateOptions(IMessageMonitor messageMonitor, ILogger logger)
        {
            return new ThrottledOptions()
            {
                MaxConcurrency = Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore,
                Logger = logger,
                MessageMonitor = messageMonitor,
                StartTimeout = Timeout.InfiniteTimeSpan,
                ProcessMessagesSequentially = false,
            };
        }
    }
}
