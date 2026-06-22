using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A builder for a subscription to a single queue that carries more than one message type. The type of
/// each inbound message is resolved from a discriminator on the wire (by default the SNS
/// <c>Subject</c>), so each message is deserialized and dispatched to the handler for its own type.
/// This class cannot be inherited.
/// </summary>
public sealed class MultiTypeQueueSubscriptionBuilder : ISubscriptionBuilder<object>
{
    private readonly string _queueName;
    private readonly List<IMessageTypeRegistration> _registrations = [];
    private Action<SqsReadConfiguration> _configureReads;

    internal MultiTypeQueueSubscriptionBuilder(string queueName)
    {
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
    }

    /// <summary>
    /// Registers a message type that can arrive on this queue, along with its handler.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="middlewareConfiguration">An optional middleware configuration for this type's handler.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public MultiTypeQueueSubscriptionBuilder Handling<TMessage>(Action<HandlerMiddlewareBuilder> middlewareConfiguration = null)
        where TMessage : class
    {
        _registrations.Add(new MessageTypeRegistration<TMessage>(middlewareConfiguration));
        return this;
    }

    /// <summary>
    /// Configures the SQS read configuration for the queue.
    /// </summary>
    /// <param name="configure">A delegate to configure SQS reads.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public MultiTypeQueueSubscriptionBuilder WithReadConfiguration(Action<SqsReadConfiguration> configure)
    {
        _configureReads = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    /// <inheritdoc />
    ISubscriptionBuilder<object> ISubscriptionBuilder<object>.WithMiddlewareConfiguration(Action<HandlerMiddlewareBuilder> middlewareConfiguration)
        => throw new NotSupportedException($"Configure middleware per message type via {nameof(Handling)}<T>(configure) on a multi-type queue subscription.");

    /// <inheritdoc />
    void ISubscriptionBuilder<object>.Configure(
        JustSayingBus bus,
        IHandlerResolver handlerResolver,
        IServiceResolver serviceResolver,
        IVerifyAmazonQueues creator,
        IAwsClientFactoryProxy awsClientFactoryProxy,
        ILoggerFactory loggerFactory)
    {
        if (_registrations.Count == 0)
        {
            throw new InvalidOperationException($"A multi-type queue subscription must handle at least one message type; call {nameof(Handling)}<T>().");
        }

        var logger = loggerFactory.CreateLogger<MultiTypeQueueSubscriptionBuilder>();

        var subscriptionConfig = new SqsReadConfiguration(SubscriptionType.PointToPoint)
        {
            QueueName = _queueName,
        };

        _configureReads?.Invoke(subscriptionConfig);

        // The queue name is explicit for a multi-type subscription, so no naming convention is applied.
        subscriptionConfig.QueueName = _queueName;
        subscriptionConfig.SubscriptionGroupName ??= subscriptionConfig.QueueName;
        subscriptionConfig.Validate();

        var config = bus.Config;
        var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

        var queue = creator.EnsureQueueExists(region, subscriptionConfig);
        bus.AddStartupTask(queue.StartupTask);

        var serializersByName = new Dictionary<string, IMessageBodySerializer>(StringComparer.Ordinal);
        foreach (var registration in _registrations)
        {
            var logicalName = bus.MessageTypeRegistry.GetLogicalName(registration.MessageType);
            serializersByName[logicalName] = registration.CreateErasedSerializer(bus);
            registration.RegisterHandler(bus, handlerResolver, serviceResolver, subscriptionConfig.QueueName);
        }

        var discriminators = new IMessageTypeDiscriminator[] { new SubjectMessageTypeDiscriminator() };
        var serializerResolver = new DiscriminatingInboundMessageSerializerResolver(discriminators, serializersByName);

        bus.AddQueue(subscriptionConfig.SubscriptionGroupName, new SqsSource
        {
            MessageConverter = new InboundMessageConverter(serializerResolver, bus.CompressionRegistry, subscriptionConfig.RawMessageDelivery),
            SqsQueue = queue.Queue,
        });

        logger.LogInformation(
            "Created multi-type SQS subscriber on queue '{QueueName}' handling {MessageTypeCount} message types.",
            subscriptionConfig.QueueName,
            _registrations.Count);
    }

    private interface IMessageTypeRegistration
    {
        Type MessageType { get; }

        IMessageBodySerializer CreateErasedSerializer(JustSayingBus bus);

        void RegisterHandler(JustSayingBus bus, IHandlerResolver handlerResolver, IServiceResolver serviceResolver, string queueName);
    }

    private sealed class MessageTypeRegistration<TMessage>(Action<HandlerMiddlewareBuilder> middlewareConfiguration)
        : IMessageTypeRegistration where TMessage : class
    {
        public Type MessageType => typeof(TMessage);

        public IMessageBodySerializer CreateErasedSerializer(JustSayingBus bus)
            => bus.MessageBodySerializerFactory.GetSerializer<TMessage>().Erase();

        public void RegisterHandler(JustSayingBus bus, IHandlerResolver handlerResolver, IServiceResolver serviceResolver, string queueName)
        {
            var resolutionContext = new HandlerResolutionContext(queueName);
            var proposedHandler = handlerResolver.ResolveHandler<TMessage>(resolutionContext)
                ?? throw new HandlerNotRegisteredWithContainerException($"There is no handler for '{typeof(TMessage)}' messages.");

            var middleware = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver)
                .Configure(middlewareConfiguration ?? (b => b.UseDefaults<TMessage>(proposedHandler.GetType())))
                .Build();

            bus.AddMessageMiddleware<TMessage>(queueName, middleware);
        }
    }
}
