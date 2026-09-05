using JustSaying.AwsTools;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
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
    private List<IPublicationBuilder<object>> Publications { get; } = [];

    private readonly List<Func<IServiceResolver, MiddlewareBase<PublishContext, bool>>> _publishMiddlewareFactories = [];

    /// <summary>
    /// Configures a publisher for a queue.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <returns>
    /// The current <see cref="PublicationsBuilder"/>.
    /// </returns>
    public PublicationsBuilder WithQueue<T>() where T : class
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
    public PublicationsBuilder WithQueue<T>(Action<QueuePublicationBuilder<T>> configure) where T : class
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
    /// Configures a publisher for a queue described by a <see cref="QueueDestination"/> destination: named by
    /// convention or explicitly (created on startup, with any configured infrastructure), or a
    /// pre-existing queue by URL or ARN (never created).
    /// </summary>
    /// <param name="destination">The queue to publish to.</param>
    /// <param name="configure">An optional delegate to configure the queue publication.</param>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    public PublicationsBuilder WithQueue<T>(QueueDestination destination, Action<QueuePublicationBuilder<T>> configure = null) where T : class
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));

        var builder = new QueuePublicationBuilder<T>(destination);
        configure?.Invoke(builder);

        Publications.Add(builder);

        return this;
    }

    /// <summary>
    /// Configures a publisher for a pre-existing queue.
    /// </summary>
    /// <param name="queueArn">The ARN of the queue to publish to.</param>
    /// <typeparam name="T">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueArn<T>(string queueArn) where T : class
        => WithQueueArn<T>(queueArn, null);

    /// <summary>
    /// Configures a publisher for a pre-existing queue.
    /// </summary>
    /// <param name="queueArn">The ARN of the queue to publish to.</param>
    /// <param name="configure">An optional delegate to configure a queue publication.</param>
    /// <typeparam name="T">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueArn<T>(string queueArn, Action<QueuePublicationBuilder<T>> configure) where T : class
    {
        if (queueArn == null) throw new ArgumentNullException(nameof(queueArn));

        return WithQueue(QueueDestination.FromArn(queueArn), configure);
    }

    /// <summary>
    /// Configures a publisher for a pre-existing queue.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to publish to.</param>
    /// <typeparam name="T">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueUrl<T>(string queueUrl) where T : class
        => WithQueueUrl<T>(queueUrl, null);

    /// <summary>
    /// Configures a publisher for a pre-existing queue.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to publish to.</param>
    /// <param name="configure">An optional delegate to configure a queue publication.</param>
    /// <typeparam name="T">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueUrl<T>(string queueUrl, Action<QueuePublicationBuilder<T>> configure) where T : class
    {
        if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));

        return WithQueue(QueueDestination.FromUrl(queueUrl), configure);
    }

    /// <summary>
    /// Configures a publisher for a pre-existing queue.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to publish to.</param>
    /// <typeparam name="T">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueUri<T>(Uri queueUrl) where T : class
        => WithQueueUri<T>(queueUrl, null);

    /// <summary>
    /// Configures a publisher for a pre-existing queue.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to publish to.</param>
    /// <param name="configure">An optional delegate to configure a queue publication.</param>
    /// <typeparam name="T">The type of the message to publish to.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationsBuilder WithQueueUri<T>(Uri queueUrl, Action<QueuePublicationBuilder<T>> configure) where T : class
    {
        if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));

        return WithQueue(QueueDestination.FromUri(queueUrl), configure);
    }

    /// <summary>
    /// Configures a publisher for a topic.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <returns>
    /// The current <see cref="PublicationsBuilder"/>.
    /// </returns>
    public PublicationsBuilder WithTopic<T>() where T : class
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
    public PublicationsBuilder WithTopic<T>(Action<TopicPublicationBuilder<T>> configure) where T : class
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
    /// Configures a publisher for a topic described by a <see cref="TopicDestination"/> destination: named by
    /// convention or explicitly (created on startup, with any configured infrastructure), or a
    /// pre-existing topic by ARN (never created).
    /// </summary>
    /// <param name="destination">The topic to publish to.</param>
    /// <param name="configure">An optional delegate to configure the topic publication.</param>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    public PublicationsBuilder WithTopic<T>(TopicDestination destination, Action<TopicPublicationBuilder<T>> configure = null) where T : class
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));

        var builder = new TopicPublicationBuilder<T>(destination);

        configure?.Invoke(builder);

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
    public PublicationsBuilder WithTopicArn<T>(string topicArn, Action<TopicPublicationBuilder<T>> configure = null) where T : class
    {
        if (topicArn == null) throw new ArgumentNullException(nameof(topicArn));

        return WithTopic(TopicDestination.FromArn(topicArn), configure);
    }

    /// <summary>
    /// Adds a publish middleware to the pipeline. The middleware will wrap all publish operations
    /// and can be used to add cross-cutting concerns such as tracing, logging, or metadata enrichment.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of middleware to add. Must be registered in the DI container.</typeparam>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    public PublicationsBuilder WithPublishMiddleware<TMiddleware>()
        where TMiddleware : MiddlewareBase<PublishContext, bool>
    {
        _publishMiddlewareFactories.Add(resolver => resolver.ResolveService<TMiddleware>());
        return this;
    }

    /// <summary>
    /// Configures the publications for the <see cref="JustSayingBus"/>.
    /// </summary>
    /// <param name="bus">The <see cref="JustSayingBus"/> to configure subscriptions for.</param>
    /// <param name="proxy">The <see cref="IAwsClientFactoryProxy"/> to use to create SQS/SNS clients with.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> logger factory to use.</param>
    internal void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory, IServiceResolver serviceResolver)
    {
        foreach (IPublicationBuilder<object> builder in Publications)
        {
            builder.Configure(bus, proxy, loggerFactory, serviceResolver);
        }

        var middlewares = _publishMiddlewareFactories
            .Select(f => f(serviceResolver))
            .Reverse()
            .ToArray();

        bus.PublishMiddleware = MiddlewareBuilder.BuildAsync(middlewares);
    }
}
