using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a topic subscription. This class cannot be inherited.
/// </summary>
/// <typeparam name="TMessage">
/// The type of the message.
/// </typeparam>
public sealed class TopicSubscriptionBuilder<TMessage> : ISubscriptionBuilder<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TopicSubscriptionBuilder{T}"/> class.
    /// </summary>
    internal TopicSubscriptionBuilder()
    { }

    /// <summary>
    /// Gets or sets the topic name.
    /// </summary>
    private string TopicName { get; set; } = string.Empty;

    private string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a delegate to a method to use to configure SNS reads.
    /// </summary>
    private Action<SqsReadConfiguration> ConfigureReads { get; set; }

    /// <summary>
    /// Gets the tags to add to the queue.
    /// </summary>
    private Dictionary<string, string> Tags { get; } = new(StringComparer.Ordinal);

    private Action<HandlerMiddlewareBuilder> MiddlewareConfiguration { get; set; }


    /// <summary>
    /// Configures that the <see cref="ITopicNamingConvention"/> will create the topic name that should be used.
    /// </summary>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    public TopicSubscriptionBuilder<TMessage> IntoDefaultTopic()
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
    public TopicSubscriptionBuilder<TMessage> WithQueueName(string name)
    {
        QueueName = name ?? throw new ArgumentNullException(nameof(name));
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
    public TopicSubscriptionBuilder<TMessage> WithTopicName(string name)
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
    public TopicSubscriptionBuilder<TMessage> WithReadConfiguration(
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
    /// Configures the SNS read configuration.
    /// </summary>
    /// <param name="configure">A delegate to a method to use to configure SNS reads.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public TopicSubscriptionBuilder<TMessage> WithReadConfiguration(Action<SqsReadConfiguration> configure)
    {
        ConfigureReads = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    /// <inheritdoc />
    public ISubscriptionBuilder<TMessage> WithMiddlewareConfiguration(Action<HandlerMiddlewareBuilder> middlewareConfiguration)
    {
        MiddlewareConfiguration = middlewareConfiguration;
        return this;
    }

    /// <summary>
    /// Creates a tag with no value that will be assigned to the SQS queue.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public TopicSubscriptionBuilder<TMessage> WithTag(string key) => WithTag(key, null);

    /// <summary>
    /// Creates a tag with a value that will be assigned to the SQS queue.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <param name="value">The value associated with this tag.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public TopicSubscriptionBuilder<TMessage> WithTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A queue tag key cannot be null or only whitespace", nameof(key));
        }

        Tags.Add(key, value ?? string.Empty);

        return this;
    }

    /// <inheritdoc />
    void ISubscriptionBuilder<TMessage>.Configure(
        JustSayingBus bus,
        IHandlerResolver handlerResolver,
        IServiceResolver serviceResolver,
        IVerifyAmazonQueues creator,
        IAwsClientFactoryProxy awsClientFactoryProxy,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicSubscriptionBuilder<TMessage>>();

        var subscriptionConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
        {
            QueueName = QueueName,
            TopicName = TopicName,
            Tags = Tags
        };

        var config = bus.Config;
        var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

        ConfigureReads?.Invoke(subscriptionConfig);

        subscriptionConfig.ApplyTopicNamingConvention<TMessage>(config.TopicNamingConvention);
        subscriptionConfig.ApplyQueueNamingConvention<TMessage>(config.QueueNamingConvention);
        subscriptionConfig.SubscriptionGroupName ??= subscriptionConfig.QueueName;
        subscriptionConfig.PublishEndpoint = subscriptionConfig.TopicName;
        subscriptionConfig.Validate();

        var queueWithStartup = creator.EnsureTopicExistsWithQueueSubscribed(
            region,
            subscriptionConfig);

        bus.AddStartupTask(queueWithStartup.StartupTask);
        bus.AddQueue(subscriptionConfig.SubscriptionGroupName, queueWithStartup.Queue);

        logger.LogInformation(
            "Created SQS topic subscription on topic '{TopicName}' and queue '{QueueName}'.",
            subscriptionConfig.TopicName,
            subscriptionConfig.QueueName);

        var resolutionContext = new HandlerResolutionContext(subscriptionConfig.QueueName);
        var proposedHandler = handlerResolver.ResolveHandler<TMessage>(resolutionContext);
        if (proposedHandler == null)
        {
            throw new HandlerNotRegisteredWithContainerException(
                $"There is no handler for '{typeof(TMessage)}' messages.");
        }

        var middlewareBuilder = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver);
        var handlerMiddleware = middlewareBuilder
            .Configure(MiddlewareConfiguration ?? (builder => builder.UseDefaults<TMessage>(proposedHandler.GetType())) )
            .Build();

        bus.AddMessageMiddleware<TMessage>(subscriptionConfig.QueueName, handlerMiddleware);

        logger.LogInformation(
            "Added a message handler for message type for '{MessageType}' on topic '{TopicName}' and queue '{QueueName}'.",
            typeof(TMessage),
            subscriptionConfig.TopicName,
            subscriptionConfig.QueueName);
    }
}
