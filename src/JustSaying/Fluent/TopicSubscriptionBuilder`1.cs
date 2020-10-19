using System;
using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.Handle;
using JustSaying.Models;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for a topic subscription. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the message.
    /// </typeparam>
    public sealed class TopicSubscriptionBuilder<T> : ISubscriptionBuilder<T>
        where T : Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicSubscriptionBuilder{T}"/> class.
        /// </summary>
        internal TopicSubscriptionBuilder()
        { }

        /// <summary>
        /// Gets or sets the topic name.
        /// </summary>
        private string TopicName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a delegate to a method to use to configure SNS reads.
        /// </summary>
        private Action<SqsReadConfiguration> ConfigureReads { get; set; }

        /// <summary>
        /// Configures that the <see cref="ITopicNamingConvention"/> will create the topic name that should be used.
        /// </summary>
        /// <returns>
        /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
        /// </returns>
        public TopicSubscriptionBuilder<T> IntoDefaultTopic()
            => WithName(string.Empty);

        /// <summary>
        /// Configures the name of the topic.
        /// </summary>
        /// <param name="name">The name of the topic to subscribe to.</param>
        /// <returns>
        /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TopicSubscriptionBuilder<T> WithName(string name)
        {
            TopicName = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }

        /// <summary>
        /// Configures the SNS read configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SNS reads.</param>
        /// <returns>
        /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public TopicSubscriptionBuilder<T> WithReadConfiguration(
            Action<SqsReadConfigurationBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new SqsReadConfigurationBuilder();

            configure(builder);

            ConfigureReads = builder.Configure;
            return this;
        }

        /// <summary>
        /// Configures the SNS read configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SNS reads.</param>
        /// <returns>
        /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public TopicSubscriptionBuilder<T> WithReadConfiguration(Action<SqsReadConfiguration> configure)
        {
            ConfigureReads = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <inheritdoc />
        void ISubscriptionBuilder<T>.Configure(
            JustSayingBus bus,
            IHandlerResolver resolver,
            IServiceResolver serviceResolver,
            IVerifyAmazonQueues creator,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<TopicSubscriptionBuilder<T>>();

            var subscriptionConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
            {
                QueueName = TopicName
            };

            ConfigureReads?.Invoke(subscriptionConfig);

            var config = bus.Config;

            subscriptionConfig.ApplyTopicNamingConvention<T>(config.TopicNamingConvention);
            subscriptionConfig.ApplyQueueNamingConvention<T>(config.QueueNamingConvention);
            subscriptionConfig.SubscriptionGroupName ??= subscriptionConfig.QueueName;
            subscriptionConfig.PublishEndpoint = subscriptionConfig.TopicName;
            subscriptionConfig.Validate();

            var queueWithStartup = creator.EnsureTopicExistsWithQueueSubscribed(
                config.Region,
                bus.SerializationRegister,
                subscriptionConfig,
                config.MessageSubjectProvider);
            bus.AddStartupTask(queueWithStartup.StartupTask);

            bus.AddQueue(subscriptionConfig.SubscriptionGroupName, queueWithStartup.Queue);

            logger.LogInformation(
                "Created SQS topic subscription on topic '{TopicName}' and queue '{QueueName}'.",
                subscriptionConfig.TopicName,
                subscriptionConfig.QueueName);

            var resolutionContext = new HandlerResolutionContext(subscriptionConfig.QueueName);
            var proposedHandler = resolver.ResolveHandler<T>(resolutionContext);
            if (proposedHandler == null)
            {
                throw new HandlerNotRegisteredWithContainerException(
                    $"There is no handler for '{typeof(T)}' messages.");
            }

            var middlewareBuilder = new HandleMiddlewareBuilder(resolver, serviceResolver);

            var handlerMiddleware = middlewareBuilder
                .Configure(subscriptionConfig.MiddlewareConfiguration)
                .Build<T>();

            /* Maybe this should be re-created for each request - should middlewares be singletons? I think so */
            bus.AddMessageHandler<T>(subscriptionConfig.QueueName, () => handlerMiddleware);

            logger.LogInformation(
                "Added a message handler for message type for '{MessageType}' on topic '{TopicName}' and queue '{QueueName}'.",
                typeof(T),
                subscriptionConfig.TopicName,
                subscriptionConfig.QueueName);
        }
    }
}
