using System;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for configuring instances of <see cref="SqsReadConfiguration"/>. This class cannot be inherited.
    /// </summary>
    public sealed class SqsReadConfigurationBuilder
    {
        /// <summary>
        /// Gets or sets the visibility timeout value to use.
        /// </summary>
        private TimeSpan? VisibilityTimeout { get; set; }

        /// <summary>
        /// Configures the visibility timeout to use.
        /// </summary>
        /// <param name="value">The value to use for the visibility timeout.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithVisibilityTimeout(TimeSpan value)
        {
            VisibilityTimeout = value;
            return this;
        }

        /// <summary>
        /// Configures the specified <see cref="SqsReadConfiguration"/>.
        /// </summary>
        /// <param name="config">The configuration to configure.</param>
        internal void Configure(SqsReadConfiguration config)
        {
            // TODO Which ones should be configurable? All, or just the important ones?
            // config.BaseQueueName = default;
            // config.BaseTopicName = default;
            // config.DeliveryDelay = default;
            // config.ErrorQueueOptOut = default;
            // config.ErrorQueueRetentionPeriod = default;
            // config.FilterPolicy = default;
            // config.InstancePosition = default;
            // config.MaxAllowedMessagesInFlight = default;
            // config.MessageBackoffStrategy = default;
            // config.MessageProcessingStrategy = default;
            // config.MessageRetention = default;
            // config.OnError = default;
            // config.PublishEndpoint = default;
            // config.QueueName = default;
            // config.RetryCountBeforeSendingToErrorQueue = default;
            // config.ServerSideEncryption = default;
            // config.Topic = default;
            // config.TopicSourceAccount = default;

            if (VisibilityTimeout.HasValue)
            {
                config.VisibilityTimeout = VisibilityTimeout.Value;
            }

            config.Validate();
        }
    }
}
