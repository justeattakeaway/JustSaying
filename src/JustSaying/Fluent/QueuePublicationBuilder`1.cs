using System;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for a queue publication. This class cannot be inherited.
    /// </summary>
    public sealed class QueuePublicationBuilder<T> : IPublicationBuilder
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueuePublicationBuilder{T}"/> class.
        /// </summary>
        internal QueuePublicationBuilder()
        {
        }

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
        public QueuePublicationBuilder<T> WithWriteConfiguration(Action<SqsWriteConfigurationBuilder> configure)
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
        void IPublicationBuilder.Configure(JustSayingFluently bus)
        {
            bus.WithSqsMessagePublisher<T>(ConfigureWrites);
        }
    }
}
