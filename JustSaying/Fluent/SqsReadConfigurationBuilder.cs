using System;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for configuring instances of <see cref="SqsReadConfiguration"/>. This class cannot be inherited.
    /// </summary>
    public sealed class SqsReadConfigurationBuilder
    {
        /// <summary>
        /// Gets or sets a value indicating whether to opt-out of error queues.
        /// </summary>
        private bool? ErrorQueueOptOut { get; set; }

        /// <summary>
        /// Gets or sets the instance position value to use.
        /// </summary>
        private int? InstancePosition { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of messages that can be inflight.
        /// </summary>
        private int? MaximumAllowedMessagesInflight { get; set; }

        /// <summary>
        /// Gets or sets the message retention value to use.
        /// </summary>
        private TimeSpan? MessageRetention { get; set; }

        /// <summary>
        /// Gets or sets the error callback to use.
        /// </summary>
        private Action<Exception, Message> OnError { get; set; }

        /// <summary>
        /// Gets or sets the topic source account Id to use.
        /// </summary>
        private string TopicSourceAccountId { get; set; }

        /// <summary>
        /// Gets or sets the visibility timeout value to use.
        /// </summary>
        private TimeSpan? VisibilityTimeout { get; set; }

        /// <summary>
        /// Configures an error handler to use.
        /// </summary>
        /// <param name="action">A delegate to a method to call when an error occurs.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public SqsReadConfigurationBuilder WithErrorHandler(Action<Exception> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            OnError = (exception, _) => action(exception);
            return this;
        }

        /// <summary>
        /// Configures an error handler to use.
        /// </summary>
        /// <param name="action">A delegate to a method to call when an error occurs.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public SqsReadConfigurationBuilder WithErrorHandler(Action<Exception, Message> action)
        {
            OnError = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>
        /// Configures that an error queue should be used.
        /// </summary>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithErrorQueue()
            => WithErrorQueueOptOut(false);

        /// <summary>
        /// Configures that no error queue should be used.
        /// </summary>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithNoErrorQueue()
            => WithErrorQueueOptOut(true);

        /// <summary>
        /// Configures whether to opt-out of an error queue.
        /// </summary>
        /// <param name="value">Whether or not to opt-out of an error queue.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithErrorQueueOptOut(bool value)
        {
            ErrorQueueOptOut = value;
            return this;
        }

        /// <summary>
        /// Configures the instance position to use.
        /// </summary>
        /// <param name="value">The value to use for the instance position.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithInstancePosition(int value)
        {
            InstancePosition = value;
            return this;
        }

        /// <summary>
        /// Configures the maximum number of messages that can be inflight at any time.
        /// </summary>
        /// <param name="value">The value to use for maximum number of inflight messages.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithMaximumMessagesInflight(int value)
        {
            MaximumAllowedMessagesInflight = value;
            return this;
        }

        /// <summary>
        /// Configures the message retention period to use.
        /// </summary>
        /// <param name="value">The value to use for the message retention.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithMessageRetention(TimeSpan value)
        {
            MessageRetention = value;
            return this;
        }

        /// <summary>
        /// Configures the account Id to use for the topic source.
        /// </summary>
        /// <param name="id">The Id of the AWS account which is the topic's source.</param>
        /// <returns>
        /// The current <see cref="SqsReadConfigurationBuilder"/>.
        /// </returns>
        public SqsReadConfigurationBuilder WithTopicSourceAccount(string id)
        {
            TopicSourceAccountId = id;
            return this;
        }

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
            // config.ErrorQueueRetentionPeriod = default;
            // config.FilterPolicy = default;
            // config.MessageBackoffStrategy = default;
            // config.MessageProcessingStrategy = default;
            // config.PublishEndpoint = default;
            // config.QueueName = default;
            // config.RetryCountBeforeSendingToErrorQueue = default;
            // config.ServerSideEncryption = default;
            // config.Topic = default;

            if (ErrorQueueOptOut.HasValue)
            {
                config.ErrorQueueOptOut = ErrorQueueOptOut.Value;
            }

            if (InstancePosition.HasValue)
            {
                config.InstancePosition = InstancePosition.Value;
            }

            if (MaximumAllowedMessagesInflight.HasValue)
            {
                config.MaxAllowedMessagesInFlight = MaximumAllowedMessagesInflight.Value;
            }

            if (MessageRetention.HasValue)
            {
                config.MessageRetention = MessageRetention.Value;
            }

            if (OnError != null)
            {
                config.OnError = OnError;
            }

            if (TopicSourceAccountId != null)
            {
                config.TopicSourceAccount = TopicSourceAccountId;
            }

            if (VisibilityTimeout.HasValue)
            {
                config.VisibilityTimeout = VisibilityTimeout.Value;
            }

            config.Validate();
        }
    }
}
