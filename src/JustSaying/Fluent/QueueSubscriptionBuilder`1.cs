using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A builder for a queue subscription's read-time behaviour. The destination — which queue, and
/// (when JustSaying owns it) how to create it — is a <see cref="QueueDestination"/> value supplied at
/// registration; this builder is the same whether the queue is created by JustSaying or already
/// exists, and exposes no infrastructure configuration. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message.
/// </typeparam>
public sealed class QueueSubscriptionBuilder<T> : ISubscriptionBuilder<T> where T : class
{
    private readonly QueueDestination _destination;

    private string QueueName { get; set; }

    private string SubscriptionGroupName { get; set; }

    private bool RawMessageDelivery { get; set; }

    private bool ShouldCheckQueueExistence { get; set; }

    private Action<HandlerMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    /// <summary>
    /// Gets or sets a serializer that overrides the per-type default from the bus's serialization factory.
    /// </summary>
    private IMessageBodySerializer<T> MessageBodySerializer { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueSubscriptionBuilder{T}"/> class.
    /// </summary>
    internal QueueSubscriptionBuilder()
        : this(QueueDestination.ByConvention())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueSubscriptionBuilder{T}"/> class for the
    /// specified destination.
    /// </summary>
    /// <param name="destination">The queue the subscription reads from.</param>
    internal QueueSubscriptionBuilder(QueueDestination destination)
    {
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    /// <summary>
    /// Configures that the <see cref="IQueueNamingConvention"/> will create the queue name that should be used.
    /// </summary>
    /// <returns>
    /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
    /// </returns>
    public QueueSubscriptionBuilder<T> WithDefaultQueue()
        => WithQueueName(string.Empty);

    /// <summary>
    /// Configures the name of the queue. Equivalent to registering the subscription against
    /// <see cref="QueueDestination.Named(string)"/>.
    /// </summary>
    /// <param name="name">The name of the queue to subscribe to.</param>
    /// <returns>
    /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public QueueSubscriptionBuilder<T> WithQueueName(string name)
    {
        QueueName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Configures the subscription group this subscription's reads are coordinated under. Defaults to
    /// the queue name.
    /// </summary>
    /// <param name="subscriptionGroupName">The name of the subscription group.</param>
    /// <returns>The current <see cref="QueueSubscriptionBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="subscriptionGroupName"/> is <see langword="null"/> or empty.</exception>
    public QueueSubscriptionBuilder<T> WithSubscriptionGroup(string subscriptionGroupName)
    {
        if (string.IsNullOrEmpty(subscriptionGroupName)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(subscriptionGroupName));

        SubscriptionGroupName = subscriptionGroupName;
        return this;
    }

    /// <summary>
    /// Declares that this queue's message bodies arrive verbatim, without JustSaying's
    /// <c>{ "Subject", "Message" }</c> envelope or the SNS notification wrapper.
    /// </summary>
    /// <returns>The current <see cref="QueueSubscriptionBuilder{T}"/>.</returns>
    public QueueSubscriptionBuilder<T> WithRawMessageDelivery()
    {
        RawMessageDelivery = true;
        return this;
    }

    /// <summary>
    /// Checks that the configured SQS queue exists before the bus starts receiving messages. Only
    /// applicable for a pre-existing queue (a queue JustSaying owns is created on startup).
    /// </summary>
    /// <returns>
    /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
    /// </returns>
    public QueueSubscriptionBuilder<T> WithQueueExistenceCheck()
    {
        ShouldCheckQueueExistence = true;
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
    /// <param name="messageBodySerializer">The serializer to deserialize this queue's message bodies with.</param>
    /// <returns>
    /// The current <see cref="QueueSubscriptionBuilder{T}"/>.
    /// </returns>
    public QueueSubscriptionBuilder<T> WithMessageBodySerializer(IMessageBodySerializer<T> messageBodySerializer)
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
        var logger = loggerFactory.CreateLogger<QueueSubscriptionBuilder<T>>();

        var serializer = MessageBodySerializer ?? bus.MessageBodySerializerFactory.GetSerializer<T>();
        var compressionRegistry = bus.CompressionRegistry;

        AwsTools.MessageHandling.ISqsQueue sqsQueue;
        string queueName;

        if (_destination.IsAddress)
        {
            if (QueueName is not null)
            {
                throw new InvalidOperationException(
                    $"A queue addressed by URL or ARN cannot also be named; remove the {nameof(WithQueueName)} call.");
            }

            var sqsClient = awsClientFactoryProxy
                .GetAwsClientFactory()
                .GetSqsClient(RegionEndpoint.GetBySystemName(_destination.Address.RegionName));

            var queue = new QueueAddressQueue(_destination.Address, sqsClient);

            if (ShouldCheckQueueExistence)
            {
                bus.AddStartupTask(async cancellationToken =>
                {
                    if (!await queue.ExistsAsync(cancellationToken).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException(
                            $"SQS queue '{queue.QueueName}' with URL '{queue.Uri}' does not exist.");
                    }
                });
            }

            sqsQueue = queue;
            queueName = queue.QueueName;
        }
        else
        {
            if (ShouldCheckQueueExistence)
            {
                throw new InvalidOperationException(
                    $"{nameof(WithQueueExistenceCheck)} only applies to a pre-existing queue; a queue JustSaying owns is created on startup.");
            }

            if (QueueName is { Length: > 0 } && _destination.Name is not null)
            {
                throw new InvalidOperationException(
                    $"The queue is named both by the {nameof(QueueDestination)} destination ('{_destination.Name}') and {nameof(WithQueueName)} ('{QueueName}'); name it once.");
            }

            var subscriptionConfig = new SqsReadConfiguration(SubscriptionType.PointToPoint)
            {
                QueueName = QueueName is { Length: > 0 } ? QueueName : _destination.Name ?? string.Empty,
                Tags = _destination.Infrastructure?.Tags ?? new Dictionary<string, string>(StringComparer.Ordinal),
                RawMessageDelivery = RawMessageDelivery,
            };

            _destination.Infrastructure?.Apply(subscriptionConfig);

            var config = bus.Config;
            var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

            subscriptionConfig.ApplyTopicNamingConvention<T>(config.TopicNamingConvention);
            subscriptionConfig.ApplyQueueNamingConvention<T>(config.QueueNamingConvention);
            subscriptionConfig.SubscriptionGroupName = SubscriptionGroupName ?? subscriptionConfig.QueueName;
            subscriptionConfig.Validate();

            var queue = creator.EnsureQueueExists(region, subscriptionConfig);
            bus.AddStartupTask(queue.StartupTask);

            sqsQueue = queue.Queue;
            queueName = subscriptionConfig.QueueName;
        }

        bus.AddQueue(SubscriptionGroupName ?? queueName, new SqsSource
        {
            MessageConverter = new InboundMessageConverter(serializer.Erase(), compressionRegistry, RawMessageDelivery),
            SqsQueue = sqsQueue,
        });

        logger.LogInformation(
            "Created SQS subscriber for message type '{MessageType}' on queue '{QueueName}'.",
            typeof(T),
            queueName);

        var resolutionContext = new HandlerResolutionContext(queueName);
        var proposedHandler = handlerResolver.ResolveHandler<T>(resolutionContext) ?? throw new HandlerNotRegisteredWithContainerException(
                $"There is no handler for '{typeof(T)}' messages.");
        var middlewareBuilder = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver);
        var handlerMiddleware = middlewareBuilder
            .Configure(MiddlewareConfiguration ?? (b => b.UseDefaults<T>(proposedHandler.GetType())))
            .Build();

        bus.AddMessageMiddleware<T>(queueName, handlerMiddleware);

        logger.LogInformation(
            "Added a message handler for message type for '{MessageType}' on queue '{QueueName}'.",
            typeof(T),
            queueName);
    }
}
