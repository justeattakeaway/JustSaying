using System;
using System.Collections.Generic;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for a queue subscription. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the message.
    /// </typeparam>
    public sealed class QueueSubscriptionBuilder<T> : ISubscriptionBuilder<T>
        where T : Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSubscriptionBuilder{T}"/> class.
        /// </summary>
        internal QueueSubscriptionBuilder()
        { }

        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        private string QueueName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a delegate to a method to use to configure SQS reads.
        /// </summary>
        private Action<SqsReadConfiguration> ConfigureReads { get; set; }

        /// <summary>
        /// Gets the tags to add to the queue.
        /// </summary>
        private Dictionary<string, string> Tags { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Configures that the <see cref="IQueueNamingConvention"/> will create the queue name that should be used.
        /// </summary>
        /// <returns>
        /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
        /// </returns>
        public QueueSubscriptionBuilder<T> WithDefaultQueue()
            => WithName(string.Empty);

        /// <summary>
        /// Configures the name of the queue.
        /// </summary>
        /// <param name="name">The name of the queue to subscribe to.</param>
        /// <returns>
        /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public QueueSubscriptionBuilder<T> WithName(string name)
        {
            QueueName = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }

        /// <summary>
        /// Configures the SQS read configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SQS reads.</param>
        /// <returns>
        /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public QueueSubscriptionBuilder<T> WithReadConfiguration(
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
        /// Configures the SQS read configuration.
        /// </summary>
        /// <param name="configure">A delegate to a method to use to configure SQS reads.</param>
        /// <returns>
        /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public QueueSubscriptionBuilder<T> WithReadConfiguration(Action<SqsReadConfiguration> configure)
        {
            ConfigureReads = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <summary>
        /// Creates a tag with no value that will be assigned to the SQS queue.
        /// </summary>
        /// <param name="key">The key for the tag.</param>
        /// <returns>
        /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="key"/> is <see langword="null"/> or whitespace.
        /// </exception>
        public QueueSubscriptionBuilder<T> WithTag(string key) => WithTag(key, null);

        /// <summary>
        /// Creates a tag with a value that will be assigned to the SQS queue.
        /// </summary>
        /// <param name="key">The key for the tag.</param>
        /// <param name="value">The value associated with this tag.</param>
        /// <returns>
        /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
        /// </returns>
        /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="key"/> is <see langword="null"/> or whitespace.
        /// </exception>
        public QueueSubscriptionBuilder<T> WithTag(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A queue tag key cannot be null or only whitespace", nameof(key));
            }

            Tags.Add(key, value ?? string.Empty);

            return this;
        }

        /// <inheritdoc />
        void ISubscriptionBuilder<T>.Configure(
            JustSayingBus bus,
            IHandlerResolver handlerResolver,
            IServiceResolver serviceResolver,
            IVerifyAmazonQueues creator,
            IAwsClientFactoryProxy awsClientFactoryProxy,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<QueueSubscriptionBuilder<T>>();

            var subscriptionConfig = new SqsReadConfiguration(SubscriptionType.PointToPoint)
            {
                QueueName = QueueName,
                Tags = Tags
            };

            ConfigureReads?.Invoke(subscriptionConfig);

            var config = bus.Config;
            var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

            subscriptionConfig.ApplyTopicNamingConvention<T>(config.TopicNamingConvention);
            subscriptionConfig.ApplyQueueNamingConvention<T>(config.QueueNamingConvention);
            subscriptionConfig.SubscriptionGroupName ??= subscriptionConfig.QueueName;
            subscriptionConfig.MiddlewareConfiguration = subscriptionConfig.MiddlewareConfiguration;
            subscriptionConfig.Validate();

            var queue = creator.EnsureQueueExists(region, subscriptionConfig);
            bus.AddStartupTask(queue.StartupTask);

            bus.AddQueue(subscriptionConfig.SubscriptionGroupName, queue.Queue);

            logger.LogInformation(
                "Created SQS subscriber for message type '{MessageType}' on queue '{QueueName}'.",
                typeof(T),
                subscriptionConfig.QueueName);

            var resolutionContext = new HandlerResolutionContext(subscriptionConfig.QueueName);
            var proposedHandler = handlerResolver.ResolveHandler<T>(resolutionContext);
            if (proposedHandler == null)
            {
                throw new HandlerNotRegisteredWithContainerException(
                    $"There is no handler for '{typeof(T)}' messages.");
            }

            var middlewareBuilder = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver);

            var handlerMiddleware = middlewareBuilder
                .UseHandler<T>()
                .UseStopwatch(proposedHandler.GetType())
                .Configure(subscriptionConfig.MiddlewareConfiguration)
                .Build();

            bus.AddMessageMiddleware<T>(subscriptionConfig.QueueName, handlerMiddleware);

            logger.LogInformation(
                "Added a message handler for message type for '{MessageType}' on topic '{TopicName}' and queue '{QueueName}'.",
                typeof(T),
                subscriptionConfig.TopicName,
                subscriptionConfig.QueueName);
        }
    }
}
