using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a queue subscription to an existing queue. This class cannot be inherited.
/// </summary>
/// <typeparam name="TMessage">
/// The type of the message.
/// </typeparam>
public sealed class QueueAddressSubscriptionBuilder<TMessage> : ISubscriptionBuilder<TMessage>
    where TMessage : class
{
    private readonly QueueAddress _queueAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueAddressSubscriptionBuilder{T}"/> class.
    /// </summary>
    /// <param name="queueAddress">The address of the queue to subscribe to.</param>
    internal QueueAddressSubscriptionBuilder(QueueAddress queueAddress)
    {
        _queueAddress = queueAddress;
    }

    private Action<QueueAddressConfiguration> ConfigureReads { get; set; }

    private Action<HandlerMiddlewareBuilder> MiddlewareConfiguration { get; set; }


    /// <summary>
    /// Configures the SQS read configuration.
    /// </summary>
    /// <param name="configure">A delegate to configure SQS reads.</param>
    /// <returns>
    /// The current <see cref="QueueAddressSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public QueueAddressSubscriptionBuilder<TMessage> WithReadConfiguration(Action<QueueAddressConfiguration> configure)
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

    /// <inheritdoc />
    void ISubscriptionBuilder<TMessage>.Configure(
        JustSayingBus bus,
        IHandlerResolver handlerResolver,
        IServiceResolver serviceResolver,
        IVerifyAmazonQueues creator,
        IAwsClientFactoryProxy awsClientFactoryProxy,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<QueueSubscriptionBuilder<TMessage>>();

        var attachedQueueConfig = new QueueAddressConfiguration();

        ConfigureReads?.Invoke(attachedQueueConfig);

        IAmazonSQS sqsClient = awsClientFactoryProxy
            .GetAwsClientFactory()
            .GetSqsClient(RegionEndpoint.GetBySystemName(_queueAddress.RegionName));

        var queue = new QueueAddressQueue(_queueAddress, sqsClient);

        attachedQueueConfig.SubscriptionGroupName ??= queue.QueueName;
        attachedQueueConfig.Validate();

        bus.AddQueue(attachedQueueConfig.SubscriptionGroupName, queue);

        logger.LogInformation(
            "Created SQS queue subscription for '{MessageType}' on '{QueueName}'",
            typeof(TMessage), queue.QueueName);

        var resolutionContext = new HandlerResolutionContext(queue.QueueName);
        var proposedHandler = handlerResolver.ResolveHandler<TMessage>(resolutionContext);
        if (proposedHandler == null)
        {
            throw new HandlerNotRegisteredWithContainerException(
                $"There is no handler for '{typeof(TMessage)}' messages.");
        }

        var middlewareBuilder = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver);
        var handlerMiddleware = middlewareBuilder
            .Configure(MiddlewareConfiguration ?? (b => b.UseDefaults<TMessage>(proposedHandler.GetType())))
            .Build();

        bus.AddMessageMiddleware<TMessage>(queue.QueueName, handlerMiddleware);

        logger.LogInformation(
            "Added a message handler for message type for '{MessageType}' on queue '{QueueName}'",
            typeof(TMessage), queue.QueueName);
    }
}
