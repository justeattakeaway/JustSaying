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
/// each inbound message is resolved from a discriminator on the wire — by default the SNS
/// <c>Subject</c>, but the chain is extensible (for example a CloudEvents <c>type</c> discriminator) —
/// so each message is deserialized and dispatched to the handler for its own type. This class cannot
/// be inherited.
/// </summary>
public sealed class MultiTypeQueueSubscriptionBuilder : ISubscriptionBuilder<object>
{
    private readonly string _queueName;
    private readonly List<IMessageTypeRegistration> _registrations = [];
    private readonly List<IMessageTypeDiscriminator> _discriminators = [];
    private Action<SqsReadConfiguration> _configureReads;

    internal MultiTypeQueueSubscriptionBuilder(string queueName)
    {
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
    }

    /// <summary>
    /// Registers a message type that can arrive on this queue, along with its handler.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="typeName">
    /// The value the discriminator emits on the wire for this type (for example a CloudEvents
    /// <c>type</c>). When <see langword="null"/>, the type's logical name (the SNS <c>Subject</c>) is used.
    /// </param>
    /// <param name="middlewareConfiguration">An optional middleware configuration for this type's handler.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public MultiTypeQueueSubscriptionBuilder Handling<TMessage>(string typeName = null, Action<HandlerMiddlewareBuilder> middlewareConfiguration = null)
        where TMessage : class
    {
        _registrations.Add(new MessageTypeRegistration<TMessage>(typeName, middlewareConfiguration));
        return this;
    }

    /// <summary>
    /// Registers a message type that can arrive on this queue, with a custom serializer built from the
    /// bus's serialization factory rather than resolved from it directly. This is the seam a wrapping
    /// message type (such as a CloudEvents envelope) uses to deserialize into a richer shape than the
    /// factory's per-type default.
    /// </summary>
    /// <typeparam name="TMessage">The message type the handler receives.</typeparam>
    /// <param name="typeName">The value the discriminator emits on the wire for this type, or <see langword="null"/> to derive it via <paramref name="typeNameResolver"/>.</param>
    /// <param name="serializerFactory">Builds the serializer for <typeparamref name="TMessage"/> from the bus's serialization factory.</param>
    /// <param name="typeNameResolver">Derives the wire type name from the bus's serialization factory when <paramref name="typeName"/> is <see langword="null"/> (for example, a CloudEvents <c>type</c> from configuration).</param>
    /// <param name="middlewareConfiguration">An optional middleware configuration for this type's handler.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    /// <remarks>
    /// Internal extensibility seam used by serializer packages (such as JustSaying.CloudEvents, which
    /// exposes it via <c>HandlingCloudEvent&lt;T&gt;</c>); not part of the public surface.
    /// </remarks>
    internal MultiTypeQueueSubscriptionBuilder Handling<TMessage>(
        string typeName,
        Func<IMessageBodySerializationFactory, IMessageBodySerializer<TMessage>> serializerFactory,
        Func<IMessageBodySerializationFactory, string> typeNameResolver = null,
        Action<HandlerMiddlewareBuilder> middlewareConfiguration = null)
        where TMessage : class
    {
        if (serializerFactory is null) throw new ArgumentNullException(nameof(serializerFactory));
        _registrations.Add(new MessageTypeRegistration<TMessage>(typeName, middlewareConfiguration, serializerFactory, typeNameResolver));
        return this;
    }

    /// <summary>
    /// Adds a discriminator to the chain used to resolve an inbound message's type. Discriminators are
    /// tried in the order added; the first to yield a registered type name wins. When none are added,
    /// the SNS <c>Subject</c> is used.
    /// </summary>
    /// <param name="discriminator">The discriminator to add.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public MultiTypeQueueSubscriptionBuilder WithDiscriminator(IMessageTypeDiscriminator discriminator)
    {
        _discriminators.Add(discriminator ?? throw new ArgumentNullException(nameof(discriminator)));
        return this;
    }

    /// <summary>
    /// Adds a discriminator of type <typeparamref name="TDiscriminator"/> to the chain unless one is
    /// already present, so a registration helper can guarantee the discriminator it needs is configured
    /// without duplicating it. Internal extensibility seam used by serializer packages (such as
    /// JustSaying.CloudEvents, whose <c>HandlingCloudEvent&lt;T&gt;</c> ensures a
    /// <c>CloudEventTypeDiscriminator</c>).
    /// </summary>
    internal MultiTypeQueueSubscriptionBuilder EnsureDiscriminator<TDiscriminator>(Func<TDiscriminator> factory)
        where TDiscriminator : IMessageTypeDiscriminator
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        foreach (var existing in _discriminators)
        {
            if (existing is TDiscriminator)
            {
                return this;
            }
        }

        _discriminators.Add(factory());
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
        => throw new NotSupportedException($"Configure middleware per message type via {nameof(Handling)}<T>(typeName, configure) on a multi-type queue subscription.");

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
            var typeName = registration.ResolveTypeName(bus);
            serializersByName[typeName] = registration.CreateErasedSerializer(bus);
            registration.RegisterHandler(bus, handlerResolver, serviceResolver, subscriptionConfig.QueueName);
        }

        var discriminators = _discriminators.Count > 0
            ? _discriminators.ToArray()
            : [new SubjectMessageTypeDiscriminator()];
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

        string ResolveTypeName(JustSayingBus bus);

        IMessageBodySerializer CreateErasedSerializer(JustSayingBus bus);

        void RegisterHandler(JustSayingBus bus, IHandlerResolver handlerResolver, IServiceResolver serviceResolver, string queueName);
    }

    private sealed class MessageTypeRegistration<TMessage>(
        string typeName,
        Action<HandlerMiddlewareBuilder> middlewareConfiguration,
        Func<IMessageBodySerializationFactory, IMessageBodySerializer<TMessage>> serializerFactory = null,
        Func<IMessageBodySerializationFactory, string> typeNameResolver = null)
        : IMessageTypeRegistration where TMessage : class
    {
        public Type MessageType => typeof(TMessage);

        // Precedence: an explicit type name wins; otherwise a resolver (e.g. the configured CloudEvents
        // `type`); otherwise the type's logical name (the SNS Subject).
        public string ResolveTypeName(JustSayingBus bus)
            => typeName
               ?? typeNameResolver?.Invoke(bus.MessageBodySerializerFactory)
               ?? bus.MessageTypeRegistry.GetLogicalName(typeof(TMessage));

        public IMessageBodySerializer CreateErasedSerializer(JustSayingBus bus)
            => (serializerFactory is null
                ? bus.MessageBodySerializerFactory.GetSerializer<TMessage>()
                : serializerFactory(bus.MessageBodySerializerFactory)).Erase();

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
