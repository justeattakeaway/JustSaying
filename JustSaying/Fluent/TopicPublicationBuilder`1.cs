using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;

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
        public TopicPublicationBuilder<T> WithWriteConfiguration(Action<SnsWriteConfiguration> configure)
        {
            ConfigureWrites = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <inheritdoc />
        void IPublicationBuilder<T>.Configure(JustSayingFluently bus)
        {
            bus.WithSnsMessagePublisher<T>(ConfigureWrites);
        }
    }
}
