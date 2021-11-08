using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// Defines the base class for a builder for instances of <typeparamref name="TConfiguration"/>.
    /// </summary>
    /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
    /// <typeparam name="TBuilder">The type of the builder.</typeparam>
    public abstract class SqsConfigurationBuilder<TConfiguration, TBuilder>
        where TConfiguration : SqsBasicConfiguration
        where TBuilder : SqsConfigurationBuilder<TConfiguration, TBuilder>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqsConfigurationBuilder{TConfiguration, TBuilder}"/> class.
        /// </summary>
        internal SqsConfigurationBuilder()
        {
        }

        /// <summary>
        /// Gets the current <typeparamref name="TBuilder"/>.
        /// </summary>
        protected abstract TBuilder Self { get; }

        /// <summary>
        /// Gets or sets the server-side encryption to use, if any.
        /// </summary>
        private ServerSideEncryption Encryption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to opt-out of error queues.
        /// </summary>
        private bool? ErrorQueueOptOut { get; set; }

        /// <summary>
        /// Gets or sets the message retention value to use.
        /// </summary>
        private TimeSpan? MessageRetention { get; set; }

        /// <summary>
        /// Gets or sets the visibility timeout value to use.
        /// </summary>
        private TimeSpan? VisibilityTimeout { get; set; }

        /// <summary>
        /// Configures that server-side encryption should be used.
        /// </summary>
        /// <param name="masterKeyId">The Id of the KMS master key to use.</param>
        /// <returns>
        /// The current <typeparamref name="TBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="masterKeyId"/> is <see langword="null"/>.
        /// </exception>
        public TBuilder WithEncryption(string masterKeyId)
        {
            if (masterKeyId == null)
            {
                throw new ArgumentNullException(nameof(masterKeyId));
            }

            if (Encryption == null)
            {
                Encryption = new ServerSideEncryption();
            }

            Encryption.KmsMasterKeyId = masterKeyId;
            return Self;
        }

        /// <summary>
        /// Configures that server-side encryption should be used.
        /// </summary>
        /// <param name="encryption">The server-side encryption configuration to use.</param>
        /// <returns>
        /// The current <typeparamref name="TBuilder"/>.
        /// </returns>
        public TBuilder WithEncryption(ServerSideEncryption encryption)
        {
            Encryption = encryption;
            return Self;
        }

        /// <summary>
        /// Configures that an error queue should be used.
        /// </summary>
        /// <returns>
        /// The current <typeparamref name="TBuilder"/>.
        /// </returns>
        public TBuilder WithErrorQueue()
            => WithErrorQueueOptOut(false);

        /// <summary>
        /// Configures that no error queue should be used.
        /// </summary>
        /// <returns>
        /// The current <typeparamref name="TBuilder"/>.
        /// </returns>
        public TBuilder WithNoErrorQueue()
            => WithErrorQueueOptOut(true);

        /// <summary>
        /// Configures whether to opt-out of an error queue.
        /// </summary>
        /// <param name="value">Whether or not to opt-out of an error queue.</param>
        /// <returns>
        /// The current <typeparamref name="TBuilder"/>.
        /// </returns>
        public TBuilder WithErrorQueueOptOut(bool value)
        {
            ErrorQueueOptOut = value;
            return Self;
        }

        /// <summary>
        /// Configures the message retention period to use.
        /// </summary>
        /// <param name="value">The value to use for the message retention.</param>
        /// <returns>
        /// The current <typeparamref name="TBuilder"/>.
        /// </returns>
        public TBuilder WithMessageRetention(TimeSpan value)
        {
            MessageRetention = value;
            return Self;
        }

        /// <summary>
        /// Configures the visibility timeout to use.
        /// </summary>
        /// <param name="value">The value to use for the visibility timeout.</param>
        /// <returns>
        /// The current <typeparamref name="TBuilder"/>.
        /// </returns>
        public TBuilder WithVisibilityTimeout(TimeSpan value)
        {
            VisibilityTimeout = value;
            return Self;
        }

        /// <summary>
        /// Configures the specified <see cref="TConfiguration"/>.
        /// </summary>
        /// <param name="config">The configuration to configure.</param>
        internal virtual void Configure(TConfiguration config)
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
            // config.RetryCountBeforeSendingToErrorQueue = default;
            // config.Topic = default;

            if (Encryption != null)
            {
                config.ServerSideEncryption = Encryption;
            }

            if (ErrorQueueOptOut.HasValue)
            {
                config.ErrorQueueOptOut = ErrorQueueOptOut.Value;
            }

            if (MessageRetention.HasValue)
            {
                config.MessageRetention = MessageRetention.Value;
            }

            if (VisibilityTimeout.HasValue)
            {
                config.VisibilityTimeout = VisibilityTimeout.Value;
            }
        }
    }
}
