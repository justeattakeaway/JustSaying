using System;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for a topic subscription. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the message.
    /// </typeparam>
    public sealed class TopicSubscriptionBuilder<T> : ISubscriptionBuilder
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicSubscriptionBuilder{T}"/> class.
        /// </summary>
        internal TopicSubscriptionBuilder()
        {
        }

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
        public TopicSubscriptionBuilder<T> WithReadConfiguration(Action<SqsReadConfigurationBuilder> configure)
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
        void ISubscriptionBuilder.Configure(JustSayingFluently bus, IHandlerResolver resolver)
        {
            var topic = bus.WithSqsTopicSubscriber()
                           .IntoQueue(TopicName);

            if (ConfigureReads != null)
            {
                topic.ConfigureSubscriptionWith(ConfigureReads);
            }

            topic.WithMessageHandler<T>(resolver);
        }
    }
}
