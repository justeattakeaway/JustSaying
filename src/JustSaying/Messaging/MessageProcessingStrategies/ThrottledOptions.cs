using System;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    /// <summary>
    /// A class representing options for the <see cref="Throttled"/> class.
    /// </summary>
    public class ThrottledOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of messages to process concurrently.
        /// </summary>
        public int MaxConcurrency { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to process messages sequentially
        /// instead of dispatching them to the thread pool to process in parallel.
        /// </summary>
        /// <remarks>
        /// By default, batches of received messages are processed in parallel.
        /// </remarks>
        public bool ProcessMessagesSequentially { get; set; }

        /// <summary>
        /// Gets or sets the timeout for starting to process a message.
        /// </summary>
        public TimeSpan StartTimeout { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IMessageMonitor"/> to use.
        /// </summary>
        public IMessageMonitor MessageMonitor { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ILogger"/> to use.
        /// </summary>
        public ILogger Logger { get; set; }
    }
}
