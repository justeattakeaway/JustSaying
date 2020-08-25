using System;
using System.Threading.Tasks;
using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for a topic publication. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the message.
    /// </typeparam>
    public sealed class TopicPublicationBuilder<T> : IPublicationBuilder<T>
        where T : Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicPublicationBuilder{T}"/> class.
        /// </summary>
        internal TopicPublicationBuilder()
        { }

        /// <summary>
        /// Gets or sets a delegate to a method to use to configure SNS writes.
        /// </summary>
        private Action<SnsWriteConfiguration> ConfigureWrites { get; set; }

        /// <summary>
        /// Configures the SNS write configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SNS writes.</param>
        /// <returns>
        /// The current <see cref="TopicPublicationBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public TopicPublicationBuilder<T> WithWriteConfiguration(
            Action<SnsWriteConfigurationBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new SnsWriteConfigurationBuilder();

            configure(builder);

            ConfigureWrites = builder.Configure;
            return this;
        }

        /// <summary>
        /// Configures the SNS write configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SNS writes.</param>
        /// <returns>
        /// The current <see cref="TopicPublicationBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public TopicPublicationBuilder<T> WithWriteConfiguration(Action<SnsWriteConfiguration> configure)
        {
            ConfigureWrites = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <inheritdoc />
        void IPublicationBuilder<T>.Configure(
            JustSayingBus bus,
            IAwsClientFactoryProxy proxy,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<TopicPublicationBuilder<T>>();

            logger.LogInformation("Adding SNS publisher for message type '{MessageType}'.",
                typeof(T));

            var config = bus.Config;

            var readConfiguration = new SqsReadConfiguration(SubscriptionType.ToTopic);
            var writeConfiguration = new SnsWriteConfiguration();
            ConfigureWrites?.Invoke(writeConfiguration);
            readConfiguration.ApplyTopicNamingConvention<T>(config.TopicNamingConvention);

            bus.SerializationRegister.AddSerializer<T>();

            foreach (var region in config.Regions)
            {
                // TODO pass region down into topic creation for when we have foreign topics so we can generate the arn
                var eventPublisher = new SnsTopicByName(
                    readConfiguration.TopicName,
                    proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
                    bus.SerializationRegister,
                    loggerFactory,
                    writeConfiguration,
                    config.MessageSubjectProvider)
                {
                    MessageResponseLogger = config.MessageResponseLogger
                };

                async Task StartupTask()
                {
                    if (writeConfiguration.Encryption != null)
                    {
                        await eventPublisher.CreateWithEncryptionAsync(writeConfiguration.Encryption)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await eventPublisher.CreateAsync().ConfigureAwait(false);
                    }

                    await eventPublisher.EnsurePolicyIsUpdatedAsync(config.AdditionalSubscriberAccounts)
                        .ConfigureAwait(false);
                }

                bus.AddStartupTask(StartupTask());

                bus.AddMessagePublisher<T>(eventPublisher, region);
            }

            logger.LogInformation(
                "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'.",
                readConfiguration.TopicName,
                typeof(T));
        }
    }
}
