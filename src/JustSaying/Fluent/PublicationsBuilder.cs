using System;
using System.Collections.Generic;
using JustSaying.AwsTools;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for publications. This class cannot be inherited.
    /// </summary>
    public sealed class PublicationsBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublicationsBuilder"/> class.
        /// </summary>
        /// <param name="parent">The <see cref="MessagingBusBuilder"/> that owns this instance.</param>
        internal PublicationsBuilder(MessagingBusBuilder parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Gets the parent of this builder.
        /// </summary>
        internal MessagingBusBuilder Parent { get; }

        /// <summary>
        /// Gets the configured publication builders.
        /// </summary>
        private IList<IPublicationBuilder<Message>> Publications { get; } = new List<IPublicationBuilder<Message>>();

        /// <summary>
        /// Configures a publisher for a queue.
        /// </summary>
        /// <typeparam name="T">The type of the message to publish.</typeparam>
        /// <returns>
        /// The current <see cref="PublicationsBuilder"/>.
        /// </returns>
        public PublicationsBuilder WithQueue<T>()
            where T : Message
        {
            Publications.Add(new QueuePublicationBuilder<T>());
            return this;
        }

        /// <summary>
        /// Configures a publisher for a queue.
        /// </summary>
        /// <typeparam name="T">The type of the message to publish.</typeparam>
        /// <param name="configure">A delegate to a method to use to configure a queue.</param>
        /// <returns>
        /// The current <see cref="PublicationsBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public PublicationsBuilder WithQueue<T>(Action<QueuePublicationBuilder<T>> configure)
            where T : Message
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new QueuePublicationBuilder<T>();

            configure(builder);

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a pre-existing queue.
        /// </summary>
        /// <param name="queueAddress">The address of the queue to publish to.</param>
        /// <typeparam name="T">The type of the message to publish to.</typeparam>
        /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public PublicationsBuilder WithQueue<T>(QueueAddress queueAddress)
            where T : Message
        {
            if (queueAddress == null) throw new ArgumentNullException(nameof(queueAddress));

            IPublicationBuilder<T> builder =
                queueAddress == QueueAddress.None
                ? new QueuePublicationBuilder<T>()
                : new QueueAddressPublicationBuilder<T>(queueAddress);

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a pre-existing topic.
        /// </summary>
        /// <param name="queueArn">The ARN of the queue to publish to.</param>
        /// <typeparam name="T">The type of the message to publish to.</typeparam>
        /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public PublicationsBuilder WithQueueArn<T>(string queueArn)
            where T : Message
        {
            if (queueArn == null) throw new ArgumentNullException(nameof(queueArn));

            var builder = new QueueAddressPublicationBuilder<T>(QueueAddress.FromArn(queueArn));

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a pre-existing topic.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to publish to.</param>
        /// <typeparam name="T">The type of the message to publish to.</typeparam>
        /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public PublicationsBuilder WithQueueUrl<T>(string queueUrl)
            where T : Message
        {
            if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));

            var builder = new QueueAddressPublicationBuilder<T>(QueueAddress.FromUrl(queueUrl));

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a pre-existing topic.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to publish to.</param>
        /// <typeparam name="T">The type of the message to publish to.</typeparam>
        /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public PublicationsBuilder WithQueueUri<T>(Uri queueUrl)
            where T : Message
        {
            if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));

            var builder = new QueueAddressPublicationBuilder<T>(QueueAddress.FromUri(queueUrl));

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a topic.
        /// </summary>
        /// <typeparam name="T">The type of the message to publish.</typeparam>
        /// <returns>
        /// The current <see cref="PublicationsBuilder"/>.
        /// </returns>
        public PublicationsBuilder WithTopic<T>()
            where T : Message
        {
            Publications.Add(new TopicPublicationBuilder<T>());
            return this;
        }

        /// <summary>
        /// Configures a publisher for a topic.
        /// </summary>
        /// <typeparam name="T">The type of the message to publish.</typeparam>
        /// <param name="configure">A delegate to a method to use to configure a topic.</param>
        /// <returns>
        /// The current <see cref="PublicationsBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public PublicationsBuilder WithTopic<T>(Action<TopicPublicationBuilder<T>> configure)
            where T : Message
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new TopicPublicationBuilder<T>();

            configure(builder);

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a pre-existing topic.
        /// </summary>
        /// <param name="topicAddress">The address of the topic to publish to.</param>
        /// <typeparam name="T">The type of the message to publish to.</typeparam>
        /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public PublicationsBuilder WithTopic<T>(TopicAddress topicAddress)
            where T : Message
        {
            if (topicAddress == null) throw new ArgumentNullException(nameof(topicAddress));

            IPublicationBuilder<T> builder =
                topicAddress == TopicAddress.None
                ? new TopicPublicationBuilder<T>()
                : new TopicAddressPublicationBuilder<T>(topicAddress);

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a pre-existing topic.
        /// </summary>
        /// <param name="topicArn">The ARN of the topic to publish to.</param>
        /// <param name="configure">An optional delegate to configure a topic publisher.</param>
        /// <typeparam name="T">The type of the message to publish to.</typeparam>
        /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public PublicationsBuilder WithTopicArn<T>(string topicArn, Action<TopicAddressPublicationBuilder<T>> configure = null)
            where T : Message
        {
            if (topicArn == null) throw new ArgumentNullException(nameof(topicArn));

            var builder = new TopicAddressPublicationBuilder<T>(TopicAddress.FromArn(topicArn));

            configure?.Invoke(builder);

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures a publisher for a pre-existing topic.
        /// </summary>
        /// <param name="topicAddress">The address of the topic to publish to</param>
        /// <param name="configure">An optional delegate to configure a topic publisher.</param>
        /// <typeparam name="T">The type of the message to publish to.</typeparam>
        /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public PublicationsBuilder WithTopic<T>(TopicAddress topicAddress, Action<TopicAddressPublicationBuilder<T>> configure)
            where T : Message
        {
            if (topicAddress == null) throw new ArgumentNullException(nameof(topicAddress));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var builder = new TopicAddressPublicationBuilder<T>(topicAddress);

            configure(builder);

            Publications.Add(builder);

            return this;
        }

        /// <summary>
        /// Configures the publications for the <see cref="JustSayingBus"/>.
        /// </summary>
        /// <param name="bus">The <see cref="JustSayingBus"/> to configure subscriptions for.</param>
        /// <param name="proxy">The <see cref="IAwsClientFactoryProxy"/> to use to create SQS/SNS clients with.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> logger factory to use.</param>
        internal void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
        {
            foreach (IPublicationBuilder<Message> builder in Publications)
            {
                builder.Configure(bus, proxy, loggerFactory);
            }
        }
    }
}
