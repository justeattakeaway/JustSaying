using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Metadata;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A builder for a topic subscription: a queue owned by JustSaying, subscribed to an SNS topic. The
/// topic is a <see cref="TopicDestination"/> value supplied at registration (named by convention or
/// explicitly); the queue is a <see cref="QueueDestination"/> value configured via <see cref="WithQueue(QueueDestination)"/>.
/// This builder configures the subscription and read-time behaviour only. This class cannot be
/// inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message.
/// </typeparam>
public sealed class TopicSubscriptionBuilder<T> : ISubscriptionBuilder<T> where T : class
{
    private readonly TopicDestination _topic;

    private QueueDestination _queue = QueueDestination.ByConvention();

    private string TopicName { get; set; }

    private string QueueName { get; set; } = string.Empty;

    private string SubscriptionGroupName { get; set; }

    private bool RawMessageDelivery { get; set; }

    private string FilterPolicy { get; set; }

    private string TopicSourceAccount { get; set; }

    private Action<HandlerMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    /// <summary>
    /// Gets or sets a serializer that overrides the per-type default from the bus's serialization factory.
    /// </summary>
    private IMessageBodySerializer<T> MessageBodySerializer { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicSubscriptionBuilder{T}"/> class.
    /// </summary>
    internal TopicSubscriptionBuilder()
        : this(TopicDestination.ByConvention())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicSubscriptionBuilder{T}"/> class for the
    /// specified topic.
    /// </summary>
    /// <param name="topic">The topic to subscribe to.</param>
    internal TopicSubscriptionBuilder(TopicDestination topic)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
    }

    /// <summary>
    /// Configures that the <see cref="ITopicNamingConvention"/> will create the topic name that should be used.
    /// </summary>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    public TopicSubscriptionBuilder<T> IntoDefaultTopic()
        => WithQueueName(string.Empty);

    /// <summary>
    /// Configures the name of the queue that will be subscribed to.
    /// </summary>
    /// <param name="name">The name of the queue to subscribe to.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public TopicSubscriptionBuilder<T> WithQueueName(string name)
    {
        QueueName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Configures the queue that will be subscribed to, including (when named) how it is created.
    /// </summary>
    /// <param name="queue">The queue to subscribe to the topic.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="queue"/> is <see langword="null"/>.
    /// </exception>
    public TopicSubscriptionBuilder<T> WithQueue(QueueDestination queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        return this;
    }

    /// <summary>
    /// Configures the name of the topic that this queue will be subscribed to.
    /// </summary>
    /// <param name="name">The name of the topic subscribe to.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public TopicSubscriptionBuilder<T> WithTopicName(string name)
    {
        TopicName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Configures the subscription group this subscription's reads are coordinated under. Defaults to
    /// the queue name.
    /// </summary>
    /// <param name="subscriptionGroupName">The name of the subscription group.</param>
    /// <returns>The current <see cref="TopicSubscriptionBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="subscriptionGroupName"/> is <see langword="null"/> or empty.</exception>
    public TopicSubscriptionBuilder<T> WithSubscriptionGroup(string subscriptionGroupName)
    {
        if (string.IsNullOrEmpty(subscriptionGroupName)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(subscriptionGroupName));

        SubscriptionGroupName = subscriptionGroupName;
        return this;
    }

    /// <summary>
    /// Configures the SNS subscription for raw message delivery: the message body is delivered to the
    /// queue verbatim, without the SNS notification wrapper.
    /// </summary>
    /// <returns>The current <see cref="TopicSubscriptionBuilder{T}"/>.</returns>
    public TopicSubscriptionBuilder<T> WithRawMessageDelivery()
    {
        RawMessageDelivery = true;
        return this;
    }

    /// <summary>
    /// Configures an SNS subscription filter policy, so only matching messages are delivered to the queue.
    /// </summary>
    /// <param name="filterPolicy">The SNS filter policy, as JSON.</param>
    /// <returns>The current <see cref="TopicSubscriptionBuilder{T}"/>.</returns>
    public TopicSubscriptionBuilder<T> WithFilterPolicy(string filterPolicy)
    {
        FilterPolicy = filterPolicy;
        return this;
    }

    /// <summary>
    /// Configures the AWS account that owns the topic, for a cross-account subscription.
    /// </summary>
    /// <param name="accountId">The AWS account id that owns the topic.</param>
    /// <returns>The current <see cref="TopicSubscriptionBuilder{T}"/>.</returns>
    public TopicSubscriptionBuilder<T> WithTopicSourceAccount(string accountId)
    {
        TopicSourceAccount = accountId;
        return this;
    }

    /// <inheritdoc />
    public ISubscriptionBuilder<T> WithMiddlewareConfiguration(Action<HandlerMiddlewareBuilder> middlewareConfiguration)
    {
        MiddlewareConfiguration = middlewareConfiguration;
        return this;
    }

    /// <summary>
    /// Configures a serializer for this subscription's message bodies, used instead of the per-type
    /// default from the bus's serialization factory — so a single subscription can consume an envelope
    /// format (for example CloudEvents) without changing the app-wide serializer.
    /// </summary>
    /// <param name="messageBodySerializer">The serializer to deserialize this subscription's message bodies with.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    public TopicSubscriptionBuilder<T> WithMessageBodySerializer(IMessageBodySerializer<T> messageBodySerializer)
    {
        MessageBodySerializer = messageBodySerializer;
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
        var logger = loggerFactory.CreateLogger<TopicSubscriptionBuilder<T>>();

        if (_topic.IsAddress)
        {
            throw new InvalidOperationException(
                $"A topic subscription creates the topic if needed, so it cannot target a topic by ARN; use {nameof(TopicDestination)}.{nameof(TopicDestination.Named)} or the naming convention.");
        }

        if (_topic.Infrastructure is not null)
        {
            throw new InvalidOperationException(
                "A topic subscription does not create the topic's infrastructure configuration; configure it on the publication side.");
        }

        if (_queue.IsAddress)
        {
            throw new InvalidOperationException(
                $"A topic subscription creates and subscribes its own queue, so it cannot target a queue by URL or ARN; use {nameof(QueueDestination)}.{nameof(QueueDestination.Named)} or the naming convention.");
        }

        if (TopicName is not null && _topic.Name is not null)
        {
            throw new InvalidOperationException(
                $"The topic is named both by the {nameof(TopicDestination)} destination ('{_topic.Name}') and {nameof(WithTopicName)} ('{TopicName}'); name it once.");
        }

        if (QueueName is { Length: > 0 } && _queue.Name is not null)
        {
            throw new InvalidOperationException(
                $"The queue is named both by the {nameof(QueueDestination)} destination ('{_queue.Name}') and {nameof(WithQueueName)} ('{QueueName}'); name it once.");
        }

        var subscriptionConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
        {
            QueueName = QueueName is { Length: > 0 } ? QueueName : _queue.Name ?? string.Empty,
            TopicName = TopicName ?? _topic.Name ?? string.Empty,
            Tags = _queue.Infrastructure?.Tags ?? new Dictionary<string, string>(StringComparer.Ordinal),
            RawMessageDelivery = RawMessageDelivery,
            FilterPolicy = FilterPolicy,
            TopicSourceAccount = TopicSourceAccount,
        };

        _queue.Infrastructure?.Apply(subscriptionConfig);

        var config = bus.Config;
        var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

        subscriptionConfig.ApplyTopicNamingConvention<T>(config.TopicNamingConvention);
        subscriptionConfig.ApplyQueueNamingConvention<T>(config.QueueNamingConvention);
        subscriptionConfig.SubscriptionGroupName = SubscriptionGroupName ?? subscriptionConfig.QueueName;
        subscriptionConfig.PublishEndpoint = subscriptionConfig.TopicName;
        subscriptionConfig.Validate();

        var queueWithStartup = creator.EnsureTopicExistsWithQueueSubscribed(
            region,
            subscriptionConfig);

        bus.AddStartupTask(queueWithStartup.StartupTask);
        var compressionRegistry = bus.CompressionRegistry;
        var serializer = MessageBodySerializer ?? bus.MessageBodySerializerFactory.GetSerializer<T>();

        var sqsSource = new SqsSource
        {
            SqsQueue = queueWithStartup.Queue,
            MessageConverter = new InboundMessageConverter(serializer.Erase(), compressionRegistry, subscriptionConfig.RawMessageDelivery)
        };
        bus.AddQueue(subscriptionConfig.SubscriptionGroupName, sqsSource);

        logger.LogInformation(
            "Created SQS topic subscription on topic '{TopicName}' and queue '{QueueName}'.",
            subscriptionConfig.TopicName,
            subscriptionConfig.QueueName);

        var resolutionContext = new HandlerResolutionContext(subscriptionConfig.QueueName);
        var proposedHandler = handlerResolver.ResolveHandler<T>(resolutionContext) ?? throw new HandlerNotRegisteredWithContainerException($"There is no handler for '{typeof(T)}' messages.");
        var middlewareBuilder = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver);
        var handlerMiddleware = middlewareBuilder
            .Configure(MiddlewareConfiguration ?? (builder => builder.UseDefaults<T>(proposedHandler.GetType())) )
            .Build();

        bus.AddMessageMiddleware<T>(subscriptionConfig.QueueName, handlerMiddleware);

        var metadataRegistry = serviceResolver.ResolveOptionalService<IMessagingMetadataRegistry>();
        if (metadataRegistry != null)
        {
            metadataRegistry.SetRegion(region);
            metadataRegistry.AddSubscription(new SubscriptionMetadata(
                subscriptionConfig.QueueName,
                subscriptionConfig.TopicName,
                subscriptionConfig.SubscriptionGroupName,
                subscriptionConfig.RawMessageDelivery,
                [new MessageTypeMetadata(typeof(T), bus.MessageTypeRegistry.GetLogicalName(typeof(T)))]));
        }

        logger.LogInformation(
            "Added a message handler for message type for '{MessageType}' on topic '{TopicName}' and queue '{QueueName}'.",
            typeof(T),
            subscriptionConfig.TopicName,
            subscriptionConfig.QueueName);
    }
}
