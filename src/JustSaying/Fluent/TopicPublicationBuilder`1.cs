using System;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for a topic publication. This class cannot be inherited.
    /// </summary>
    public sealed class TopicPublicationBuilder<T> : IPublicationBuilder
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicPublicationBuilder{T}"/> class.
        /// </summary>
        internal TopicPublicationBuilder()
        {
        }

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
        public TopicPublicationBuilder<T> WithWriteConfiguration(Action<SnsWriteConfigurationBuilder> configure)
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
        void IPublicationBuilder.Configure(JustSayingFluently bus)
        {
            bus.WithSnsMessagePublisher<T>(ConfigureWrites);
        }
    }
}
