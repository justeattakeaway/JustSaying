using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.Models;

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
        {
        }

        /// <summary>
        /// Gets or sets the queue name.
        /// </summary>
        private string QueueName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a delegate to a method to use to configure SQS reads.
        /// </summary>
        private Action<SqsReadConfiguration> ConfigureReads { get; set; }

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
        public QueueSubscriptionBuilder<T> WithReadConfiguration(Action<SqsReadConfigurationBuilder> configure)
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

        /// <inheritdoc />
        void ISubscriptionBuilder<T>.Configure(JustSayingFluently bus, IHandlerResolver resolver)
        {
            var queue = bus.WithSqsPointToPointSubscriber()
                           .IntoQueue(QueueName);

            if (ConfigureReads != null)
            {
                queue.ConfigureSubscriptionWith(ConfigureReads);
            }

            // Need to pass in the group config so it can be configured in here...
            queue.WithMessageHandler<T>(resolver);
        }
    }
}
