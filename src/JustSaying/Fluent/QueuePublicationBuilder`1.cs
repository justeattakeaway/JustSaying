using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for a queue publication. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the message published to the queue.
    /// </typeparam>
    public sealed class QueuePublicationBuilder<T> : IPublicationBuilder<T>
        where T : Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueuePublicationBuilder{T}"/> class.
        /// </summary>
        internal QueuePublicationBuilder()
        { }

        /// <summary>
        /// Gets or sets a delegate to a method to use to configure SQS writes.
        /// </summary>
        private Action<SqsWriteConfiguration> ConfigureWrites { get; set; }

        /// <summary>
        /// Configures the SQS write configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SQS writes.</param>
        /// <returns>
        /// The current <see cref="QueuePublicationBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public QueuePublicationBuilder<T> WithWriteConfiguration(
            Action<SqsWriteConfigurationBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new SqsWriteConfigurationBuilder();

            configure(builder);

            ConfigureWrites = builder.Configure;
            return this;
        }

        /// <summary>
        /// Configures the SQS write configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SQS writes.</param>
        /// <returns>
        /// The current <see cref="QueuePublicationBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public QueuePublicationBuilder<T> WithWriteConfiguration(Action<SqsWriteConfiguration> configure)
        {
            ConfigureWrites = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <summary>
        /// Configures the SQS Queue name, rather than using the naming convention.
        /// </summary>
        /// <param name="queueName">The name of the queue to subscribe to.</param>
        /// <returns>
        /// The current <see cref="QueuePublicationBuilder{T}"/>.
        /// </returns>
        public QueuePublicationBuilder<T> WithName(string queueName)
        {
            return this.WithWriteConfiguration(r => r.WithQueueName(queueName));
        }

        /// <inheritdoc />
        void IPublicationBuilder<T>.Configure(
            JustSayingBus bus,
            IAwsClientFactoryProxy proxy,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<QueuePublicationBuilder<T>>();

            logger.LogInformation("Adding SQS publisher for message type '{MessageType}'.",
                typeof(T));

            var config = bus.Config;
            var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

            var writeConfiguration = new SqsWriteConfiguration();
            ConfigureWrites?.Invoke(writeConfiguration);
            writeConfiguration.ApplyQueueNamingConvention<T>(config.QueueNamingConvention);

            bus.SerializationRegister.AddSerializer<T>();

            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsClient = proxy.GetAwsClientFactory().GetSqsClient(regionEndpoint);

            var eventPublisher = new SqsMessagePublisher(
                sqsClient,
                bus.SerializationRegister,
                loggerFactory)
            {
                MessageResponseLogger = config.MessageResponseLogger
            };

#pragma warning disable 618
            var sqsQueue = new SqsQueueByName(
                regionEndpoint,
                writeConfiguration.QueueName,
                sqsClient,
                writeConfiguration.RetryCountBeforeSendingToErrorQueue,
                loggerFactory);
#pragma warning restore 618

            async Task StartupTask(CancellationToken cancellationToken)
            {
                if (!await sqsQueue.ExistsAsync(cancellationToken).ConfigureAwait(false))
                {
                    await sqsQueue.CreateAsync(writeConfiguration, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                eventPublisher.QueueUrl = sqsQueue.Uri;
            }

            bus.AddStartupTask(StartupTask);

            bus.AddMessagePublisher<T>(eventPublisher);

            logger.LogInformation(
                "Created SQS publisher for message type '{MessageType}' on queue '{QueueName}'.",
                typeof(T),
                writeConfiguration.QueueName);
        }
    }
}
