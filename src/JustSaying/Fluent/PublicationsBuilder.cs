using JustSaying.AwsTools;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

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
    private IList<IPublicationBuilder> Publications { get; } = new List<IPublicationBuilder>();

    /// <summary>
    /// Configures a publisher for a queue.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to publish.</typeparam>
    /// <returns>
    /// The current <see cref="PublicationsBuilder"/>.
    /// </returns>
    public PublicationsBuilder WithQueue<TMessage>()
        where TMessage : class
    {
        Publications.Add(new QueuePublicationBuilder<TMessage>());
        return this;
    }

    /// <summary>
    /// Configures a publisher for a queue.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to publish.</typeparam>
    /// <param name="configure">A delegate to a method to use to configure a queue.</param>
    /// <returns>
    /// The current <see cref="PublicationsBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public PublicationsBuilder WithQueue<TMessage>(Action<QueuePublicationBuilder<TMessage>> configure)
        where TMessage : class
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new QueuePublicationBuilder<TMessage>();

        configure(builder);

        Publications.Add(builder);

        return this;
    }

    /// <summary>
    /// Configures a publisher for a pre-existing topic.
    /// </summary>
    /// <param name="queueArn">The ARN of the queue to publish to.</param>
    /// <typeparam name="TMessage">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueArn<TMessage>(string queueArn)
        where TMessage : class
    {
        if (queueArn == null) throw new ArgumentNullException(nameof(queueArn));

        var builder = new QueueAddressPublicationBuilder<TMessage>(QueueAddress.FromArn(queueArn));

        Publications.Add(builder);

        return this;
    }

    /// <summary>
    /// Configures a publisher for a pre-existing topic.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to publish to.</param>
    /// <typeparam name="TMessage">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueUrl<TMessage>(string queueUrl)
        where TMessage : class
    {
        if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));

        var builder = new QueueAddressPublicationBuilder<TMessage>(QueueAddress.FromUrl(queueUrl));

        Publications.Add(builder);

        return this;
    }

    /// <summary>
    /// Configures a publisher for a pre-existing topic.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to publish to.</param>
    /// <typeparam name="TMessage">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueUri<TMessage>(Uri queueUrl)
        where TMessage : class
    {
        if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));

        var builder = new QueueAddressPublicationBuilder<TMessage>(QueueAddress.FromUri(queueUrl));

        Publications.Add(builder);

        return this;
    }

    /// <summary>
    /// Configures a publisher for a topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to publish.</typeparam>
    /// <returns>
    /// The current <see cref="PublicationsBuilder"/>.
    /// </returns>
    public PublicationsBuilder WithTopic<TMessage>()
        where TMessage : class
    {
        Publications.Add(new TopicPublicationBuilder<TMessage>());
        return this;
    }

    /// <summary>
    /// Configures a publisher for a topic.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to publish.</typeparam>
    /// <param name="configure">A delegate to a method to use to configure a topic.</param>
    /// <returns>
    /// The current <see cref="PublicationsBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public PublicationsBuilder WithTopic<TMessage>(Action<TopicPublicationBuilder<TMessage>> configure)
        where TMessage : class
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new TopicPublicationBuilder<TMessage>();

        configure(builder);

        Publications.Add(builder);

        return this;
    }

    /// <summary>
    /// Configures a publisher for a pre-existing topic.
    /// </summary>
    /// <param name="topicArn">The ARN of the topic to publish to.</param>
    /// <param name="configure">An optional delegate to configure a topic publisher.</param>
    /// <typeparam name="TMessage">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithTopicArn<TMessage>(string topicArn, Action<TopicAddressPublicationBuilder<TMessage>> configure = null)
        where TMessage : class
    {
        if (topicArn == null) throw new ArgumentNullException(nameof(topicArn));

        var builder = new TopicAddressPublicationBuilder<TMessage>(TopicAddress.FromArn(topicArn));

        configure?.Invoke(builder);

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
        foreach (IPublicationBuilder builder in Publications)
        {
            builder.Configure(bus, proxy, loggerFactory);
        }
    }
}
