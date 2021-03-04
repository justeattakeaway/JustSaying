using System;
using System.Threading.Tasks;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.Publishing;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
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

        /// <inheritdoc />
        void IPublicationBuilder<T>.Configure(
            JustSayingBus bus,
            IAwsClientFactoryProxy proxy,
            IMessagePublisherFactory publisherFactory,
            IQueueTopicCreatorProvider queueTopicCreatorProvider,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<QueuePublicationBuilder<T>>();

            logger.LogInformation("Adding SQS publisher for message type '{MessageType}'.",
                typeof(T));

            var config = bus.Config;

            var writeConfiguration = new SqsWriteConfiguration();
            ConfigureWrites?.Invoke(writeConfiguration);
            writeConfiguration.ApplyQueueNamingConvention<T>(config.QueueNamingConvention);

            async Task StartupTask()
            {
                var queueCreator = queueTopicCreatorProvider.GetSqsCreator(writeConfiguration.QueueName, config.Region, writeConfiguration.RetryCountBeforeSendingToErrorQueue, null);

                if (!await queueCreator.ExistsAsync().ConfigureAwait(false))
                {
                    await queueCreator.CreateAsync(writeConfiguration).ConfigureAwait(false);
                }
            }

            bus.AddStartupTask(StartupTask);

            var queuePublisher = publisherFactory.CreateSqsPublisher(writeConfiguration.QueueName, writeConfiguration.RetryCountBeforeSendingToErrorQueue);
            bus.AddMessagePublisher<T>(queuePublisher);

            logger.LogInformation(
                "Created SQS publisher for message type '{MessageType}' on queue '{QueueName}'.",
                typeof(T),
                writeConfiguration.QueueName);
        }
    }
}
