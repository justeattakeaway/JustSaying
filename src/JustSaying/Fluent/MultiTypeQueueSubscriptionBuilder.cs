using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Metadata;
using JustSaying.Messaging.Middleware;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A builder for a subscription to a single queue that carries more than one message type. The type of
/// each inbound message is resolved from a discriminator on the wire — by default the SNS
/// <c>Subject</c>, but the chain is extensible (for example a CloudEvents <c>type</c> discriminator) —
/// so each message is deserialized and dispatched to the handler for its own type. This class cannot
/// be inherited.
/// </summary>
/// <remarks>
/// The discriminator chain has a fixed order, independent of the order types and discriminators are
/// registered in: first the discriminators added with <see cref="WithDiscriminator"/> (in the order
/// added), then those added by serializer packages (such as the CloudEvents <c>type</c>
/// discriminator, which validates the body's shape), and the SNS <c>Subject</c> last. The first
/// discriminator to recognise a message decides its type; if that type isn't registered on the queue,
/// the message can't be routed, rather than falling through to a weaker signal. The Subject goes last
/// because JustSaying stamps it on every publication, CloudEvents ones included.
/// </remarks>
public sealed class MultiTypeQueueSubscriptionBuilder : ISubscriptionBuilder<object>
{
    private readonly QueueDestination _destination;
    private readonly List<IMessageTypeRegistration> _registrations = [];
    private readonly List<IMessageTypeDiscriminator> _discriminators = [];
    private readonly List<IMessageTypeDiscriminator> _packageDiscriminators = [];
    private bool _routesBySubject;
    private readonly List<TopicRegistration> _topics = [];
    private string _subscriptionGroupName;
    private bool _rawMessageDelivery;
    private bool _checkQueueExistence;

    internal MultiTypeQueueSubscriptionBuilder(QueueDestination destination)
    {
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
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
        if (typeName is null)
        {
            // With no explicit wire name, this type is routed by its logical name — the SNS Subject.
            // Make sure the Subject discriminator is in the chain even when another registration has
            // added its own (for example CloudEvents), so native and enveloped types can share a queue.
            _routesBySubject = true;
        }

        _registrations.Add(new MessageTypeRegistration<TMessage>(typeName, middlewareConfiguration));
        return this;
    }

    /// <summary>
    /// Registers a message type that can arrive on this queue, with a custom serializer built from the
    /// bus's <see cref="IServiceResolver"/> rather than resolved from the app-wide serialization
    /// factory. This is the seam a serializer package (such as CloudEvents) uses to give a registration
    /// its own serializer — resolved from the container — so one queue can mix envelope formats.
    /// </summary>
    /// <typeparam name="TMessage">The message type the handler receives.</typeparam>
    /// <param name="typeName">The value the discriminator emits on the wire for this type, or <see langword="null"/> to derive it via <paramref name="typeNameResolver"/>.</param>
    /// <param name="serializerFactory">Builds the serializer for <typeparamref name="TMessage"/> from the bus's service resolver.</param>
    /// <param name="typeNameResolver">Derives the wire type name from the bus's service resolver when <paramref name="typeName"/> is <see langword="null"/> (for example, a CloudEvents <c>type</c> from configuration).</param>
    /// <param name="middlewareConfiguration">An optional middleware configuration for this type's handler.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    /// <remarks>
    /// Internal extensibility seam used by serializer packages (such as JustSaying.CloudEvents, which
    /// exposes it via <c>HandlingCloudEvent&lt;T&gt;</c>); not part of the public surface.
    /// </remarks>
    internal MultiTypeQueueSubscriptionBuilder Handling<TMessage>(
        string typeName,
        Func<IServiceResolver, IMessageBodySerializer<TMessage>> serializerFactory,
        Func<IServiceResolver, string> typeNameResolver = null,
        Action<HandlerMiddlewareBuilder> middlewareConfiguration = null)
        where TMessage : class
    {
        if (serializerFactory is null) throw new ArgumentNullException(nameof(serializerFactory));
        _registrations.Add(new MessageTypeRegistration<TMessage>(typeName, middlewareConfiguration, serializerFactory, typeNameResolver));
        return this;
    }

    /// <summary>
    /// Adds a discriminator to the chain used to resolve an inbound message's type. Discriminators
    /// added here are tried first, in the order added — before any added by a serializer package (such
    /// as CloudEvents) and before the SNS <c>Subject</c>, which is always tried last — and the first to
    /// recognise a message decides its type. When none are added, the SNS <c>Subject</c> is used.
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
    /// without duplicating it. It is tried after any added with <see cref="WithDiscriminator"/> and
    /// before the SNS <c>Subject</c>. Internal extensibility seam used by serializer packages (such as
    /// JustSaying.CloudEvents, whose <c>HandlingCloudEvent&lt;T&gt;</c> ensures a
    /// <c>CloudEventTypeDiscriminator</c>).
    /// </summary>
    internal MultiTypeQueueSubscriptionBuilder EnsureDiscriminator<TDiscriminator>(Func<TDiscriminator> factory)
        where TDiscriminator : IMessageTypeDiscriminator
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        if (_discriminators.OfType<TDiscriminator>().Any() || _packageDiscriminators.OfType<TDiscriminator>().Any())
        {
            return this;
        }

        _packageDiscriminators.Add(factory());
        return this;
    }

    // The order is fixed rather than following registration order, so adding one type can't change
    // how another type's messages are read: the user's own discriminators, then those a serializer
    // package needs (which check the body's shape), then the SNS Subject. The Subject is stamped on
    // every JustSaying publication (a CloudEvents publication carries the payload's type name), so it
    // is only consulted once nothing more specific has recognised the message.
    private IMessageTypeDiscriminator[] BuildDiscriminatorChain()
    {
        var chain = _discriminators
            .Concat(_packageDiscriminators)
            .Where(discriminator => discriminator is not SubjectMessageTypeDiscriminator)
            .ToList();

        var subject = _discriminators.OfType<SubjectMessageTypeDiscriminator>().FirstOrDefault();
        if (subject is not null || _routesBySubject || chain.Count == 0)
        {
            chain.Add(subject ?? new SubjectMessageTypeDiscriminator());
        }

        return [.. chain];
    }

    /// <summary>
    /// Configures the subscription group this subscription's reads are coordinated under. Defaults to
    /// the queue name.
    /// </summary>
    /// <param name="subscriptionGroupName">The name of the subscription group.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="subscriptionGroupName"/> is <see langword="null"/> or empty.</exception>
    public MultiTypeQueueSubscriptionBuilder WithSubscriptionGroup(string subscriptionGroupName)
    {
        if (string.IsNullOrEmpty(subscriptionGroupName)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(subscriptionGroupName));

        _subscriptionGroupName = subscriptionGroupName;
        return this;
    }

    /// <summary>
    /// Declares that this queue's message bodies arrive verbatim, without JustSaying's
    /// <c>{ "Subject", "Message" }</c> envelope or the SNS notification wrapper.
    /// </summary>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public MultiTypeQueueSubscriptionBuilder WithRawMessageDelivery()
    {
        _rawMessageDelivery = true;
        return this;
    }

    /// <summary>
    /// Subscribes this queue to an SNS topic, as <see cref="SubscriptionsBuilder.ForTopic{T}(TopicDestination)"/>
    /// does for a single-type queue: on startup the topic is created if it does not exist, the queue is
    /// subscribed to it, and the queue's policy allows the topic to send to it. Call once for each topic
    /// the queue carries messages from. The subscription uses raw delivery when
    /// <see cref="WithRawMessageDelivery"/> is called.
    /// </summary>
    /// <param name="topic">The topic, named with <see cref="TopicDestination.Named(string)"/>.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="topic"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="topic"/> is named by convention (use <see cref="SubscribeToTopic{TMessage}()"/>),
    /// addressed by ARN, or configures the topic's infrastructure.
    /// </exception>
    public MultiTypeQueueSubscriptionBuilder SubscribeToTopic(TopicDestination topic)
        => AddTopic(topic, filterPolicy: null);

    /// <summary>
    /// Subscribes this queue to an SNS topic with a subscription filter policy, so only matching messages
    /// are delivered to the queue. See <see cref="SubscribeToTopic(TopicDestination)"/>.
    /// </summary>
    /// <param name="topic">The topic, named with <see cref="TopicDestination.Named(string)"/>.</param>
    /// <param name="filterPolicy">The SNS subscription filter policy, as JSON.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="topic"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="filterPolicy"/> is <see langword="null"/> or whitespace, or <paramref name="topic"/> is named
    /// by convention (use <see cref="SubscribeToTopic{TMessage}(string)"/>), addressed by ARN, or configures the
    /// topic's infrastructure.
    /// </exception>
    public MultiTypeQueueSubscriptionBuilder SubscribeToTopic(TopicDestination topic, string filterPolicy)
    {
        if (string.IsNullOrWhiteSpace(filterPolicy)) throw new ArgumentException("Parameter cannot be null or whitespace.", nameof(filterPolicy));

        return AddTopic(topic, filterPolicy);
    }

    /// <summary>
    /// Subscribes this queue to the SNS topic the topic naming convention names after
    /// <typeparamref name="TMessage"/>: the topic <see cref="SubscriptionsBuilder.ForTopic{T}()"/>
    /// subscribes to, and a publication of <typeparamref name="TMessage"/> publishes to by default. On
    /// startup the topic is created if it does not exist, the queue is subscribed to it, and the queue's
    /// policy allows the topic to send to it. The subscription uses raw delivery when
    /// <see cref="WithRawMessageDelivery"/> is called.
    /// </summary>
    /// <typeparam name="TMessage">The message type the topic is named after.</typeparam>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public MultiTypeQueueSubscriptionBuilder SubscribeToTopic<TMessage>()
        where TMessage : class
        => AddTopic<TMessage>(filterPolicy: null);

    /// <summary>
    /// Subscribes this queue to the SNS topic the topic naming convention names after
    /// <typeparamref name="TMessage"/>, with a subscription filter policy, so only matching messages are
    /// delivered to the queue. See <see cref="SubscribeToTopic{TMessage}()"/>.
    /// </summary>
    /// <typeparam name="TMessage">The message type the topic is named after.</typeparam>
    /// <param name="filterPolicy">The SNS subscription filter policy, as JSON.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="filterPolicy"/> is <see langword="null"/> or whitespace.</exception>
    public MultiTypeQueueSubscriptionBuilder SubscribeToTopic<TMessage>(string filterPolicy)
        where TMessage : class
    {
        if (string.IsNullOrWhiteSpace(filterPolicy)) throw new ArgumentException("Parameter cannot be null or whitespace.", nameof(filterPolicy));

        return AddTopic<TMessage>(filterPolicy);
    }

    private MultiTypeQueueSubscriptionBuilder AddTopic<TMessage>(string filterPolicy)
        where TMessage : class
    {
        _topics.Add(new TopicRegistration(convention => convention.Apply<TMessage>(null), filterPolicy, typeof(TMessage)));
        return this;
    }

    private MultiTypeQueueSubscriptionBuilder AddTopic(TopicDestination topic, string filterPolicy)
    {
        if (topic == null) throw new ArgumentNullException(nameof(topic));

        if (topic.IsAddress)
        {
            throw new ArgumentException(
                $"Subscribing a queue to a topic creates the topic if needed, so it cannot target a topic by ARN; use {nameof(TopicDestination)}.{nameof(TopicDestination.Named)}(...).",
                nameof(topic));
        }

        if (topic.Name is null)
        {
            // The naming convention names a topic after a message type, which a destination alone doesn't have.
            throw new ArgumentException(
                $"A topic named by convention needs a message type to name it after: use {nameof(SubscribeToTopic)}<T>() for the topic {nameof(SubscriptionsBuilder.ForTopic)}<T>() subscribes to, or {nameof(TopicDestination)}.{nameof(TopicDestination.Named)}(...).",
                nameof(topic));
        }

        if (topic.Infrastructure is not null)
        {
            throw new ArgumentException(
                "A topic subscription does not create the topic's infrastructure configuration; configure it on the publication side.",
                nameof(topic));
        }

        var name = topic.Name;
        _topics.Add(new TopicRegistration(_ => name, filterPolicy, conventionType: null));
        return this;
    }

    /// <summary>
    /// Checks that the configured SQS queue exists before the bus starts receiving messages. Only
    /// applicable for a pre-existing queue (a queue JustSaying owns is created on startup).
    /// </summary>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public MultiTypeQueueSubscriptionBuilder WithQueueExistenceCheck()
    {
        _checkQueueExistence = true;
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

        // The discriminator value is what routes an inbound message to a serializer, so a blank or
        // duplicated one is a misconfiguration that would otherwise silently deserialize messages as the
        // wrong type. Resolve the names up front, before any queue is created or looked up.
        var namesByRegistration = new Dictionary<IMessageTypeRegistration, string>();
        var typesByName = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var registration in _registrations)
        {
            var typeName = registration.ResolveTypeName(bus, serviceResolver);

            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new InvalidOperationException(
                    $"The message type '{registration.MessageType.ToReadableFullName()}' registered on the multi-type queue subscription for '{_destination.Name ?? _destination.Address?.QueueUrl?.ToString()}' " +
                    $"resolved to a null or empty type name. Pass an explicit name to {nameof(Handling)}<T>(typeName).");
            }

            if (typesByName.TryGetValue(typeName, out var existingType))
            {
                throw new InvalidOperationException(
                    $"The message types '{existingType.ToReadableFullName()}' and '{registration.MessageType.ToReadableFullName()}' registered on the multi-type queue subscription for " +
                    $"'{_destination.Name ?? _destination.Address?.QueueUrl?.ToString()}' both resolve to the type name '{typeName}'. Each type on a queue must have a distinct name; " +
                    $"pass an explicit name to {nameof(Handling)}<T>(typeName).");
            }

            typesByName[typeName] = registration.MessageType;
            namesByRegistration[registration] = typeName;
        }

        var discriminators = BuildDiscriminatorChain();

        // Raw delivery strips the SNS envelope, and with it the Subject, so a type routed by Subject could
        // never be resolved: every message of that type would end up in the error queue.
        if (_rawMessageDelivery && (_routesBySubject || discriminators.All(discriminator => discriminator is SubjectMessageTypeDiscriminator)))
        {
            throw new InvalidOperationException(
                $"The multi-type queue subscription for '{_destination.Name ?? _destination.Address?.QueueUrl?.ToString()}' uses raw message delivery but routes messages by the SNS Subject, " +
                $"which raw messages don't carry. Turn off raw message delivery, or give every type registered with {nameof(Handling)}<T>() an explicit type name " +
                $"read from the message body or attributes by a discriminator added with {nameof(WithDiscriminator)}(...).");
        }

        var config = bus.Config;
        ISqsQueue sqsQueue;
        string queueName;
        string region;
        if (_destination.IsAddress)
        {
            if (_topics.Count > 0)
            {
                throw new InvalidOperationException(
                    $"A queue subscribed to topics is created and subscribed by JustSaying, so it cannot be addressed by URL or ARN; use {nameof(QueueDestination)}.{nameof(QueueDestination.Named)}(...).");
            }

            // A pre-existing queue: never created, so only the read-time settings apply.
            var sqsClient = awsClientFactoryProxy
                .GetAwsClientFactory()
                .GetSqsClient(Amazon.RegionEndpoint.GetBySystemName(_destination.Address.RegionName));

            var queue = new QueueAddressQueue(_destination.Address, sqsClient);
            bus.AddSubscribedQueue(SubscribedQueue.Addressed(queue, config.Region, typesByName.Values, isMultiType: true));

            if (_checkQueueExistence)
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
            region = _destination.Address.RegionName;
        }
        else
        {
            if (_checkQueueExistence)
            {
                throw new InvalidOperationException(
                    $"{nameof(WithQueueExistenceCheck)} only applies to a pre-existing queue; a queue JustSaying owns is created on startup.");
            }

            region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");
            queueName = _destination.Name;

            // The queue name is explicit for a multi-type subscription, so no naming convention is applied.
            SqsReadConfiguration CreateQueueConfiguration(SubscriptionType subscriptionType)
            {
                var queueConfig = new SqsReadConfiguration(subscriptionType)
                {
                    QueueName = _destination.Name,
                    Tags = _destination.Infrastructure?.Tags ?? new Dictionary<string, string>(StringComparer.Ordinal),
                    RawMessageDelivery = _rawMessageDelivery,
                };

                _destination.Infrastructure?.Apply(queueConfig);

                queueConfig.SubscriptionGroupName = _subscriptionGroupName ?? queueConfig.QueueName;
                return queueConfig;
            }

            if (_topics.Count == 0)
            {
                var subscriptionConfig = CreateQueueConfiguration(SubscriptionType.PointToPoint);
                subscriptionConfig.Validate($"multi-type queue subscription to queue '{queueName}'");
                bus.AddSubscribedQueue(SubscribedQueue.Owned(region, queueName, typesByName.Values, isMultiType: true, topics: []));

                var queue = creator.EnsureQueueExists(region, subscriptionConfig);
                bus.AddStartupTask(queue.StartupTask);
                sqsQueue = queue.Queue;
            }
            else
            {
                // Each topic is subscribed the way a single-type topic subscription subscribes its queue:
                // the queue (and its error queue) is created if needed, then the topic, the SNS subscription
                // with its filter policy, and the queue policy allowing the topic to send to the queue.
                var topicConfigs = new List<SqsReadConfiguration>();
                var subscribedTopics = new List<SubscribedTopic>();
                foreach (var topic in _topics)
                {
                    var topicConfig = CreateQueueConfiguration(SubscriptionType.ToTopic);
                    topicConfig.TopicName = topic.ResolveName(config.TopicNamingConvention);
                    topicConfig.PublishEndpoint = topicConfig.TopicName;
                    topicConfig.FilterPolicy = topic.FilterPolicy;
                    topicConfig.Validate($"multi-type queue subscription to queue '{queueName}' from topic '{topicConfig.TopicName}'");

                    var subscribedTopic = new SubscribedTopic(topicConfig.TopicName, null, topic.ConventionType);
                    if (subscribedTopics.Any(subscribedTopic.IsSameTopicAs))
                    {
                        throw new InvalidOperationException(
                            $"The multi-type queue subscription for '{queueName}' subscribes to the topic '{topicConfig.TopicName}' more than once; subscribe to each topic once.");
                    }

                    subscribedTopics.Add(subscribedTopic);
                    topicConfigs.Add(topicConfig);
                }

                bus.AddSubscribedQueue(SubscribedQueue.Owned(region, queueName, typesByName.Values, isMultiType: true, topics: subscribedTopics));

                sqsQueue = null;
                foreach (var topicConfig in topicConfigs)
                {
                    var queue = creator.EnsureTopicExistsWithQueueSubscribed(region, topicConfig);
                    bus.AddStartupTask(queue.StartupTask);
                    sqsQueue ??= queue.Queue;
                }
            }
        }

        var serializersByName = new Dictionary<string, IMessageBodySerializer>(StringComparer.Ordinal);
        foreach (var registration in _registrations)
        {
            serializersByName[namesByRegistration[registration]] = registration.CreateErasedSerializer(bus, serviceResolver);
            registration.RegisterHandler(bus, handlerResolver, serviceResolver, queueName);
        }

        var serializerResolver = new DiscriminatingInboundMessageSerializerResolver(discriminators, serializersByName);

        bus.AddQueue(_subscriptionGroupName ?? queueName, new SqsSource
        {
            MessageConverter = new InboundMessageConverter(serializerResolver, bus.CompressionRegistry, _rawMessageDelivery),
            SqsQueue = sqsQueue,
        });

        var metadataRegistry = serviceResolver.ResolveOptionalService<IMessagingMetadataRegistry>();
        if (metadataRegistry != null)
        {
            metadataRegistry.SetRegion(region);
            metadataRegistry.AddSubscription(new SubscriptionMetadata(
                queueName,
                topicName: null,
                _subscriptionGroupName ?? queueName,
                _rawMessageDelivery,
                [.. _registrations.Select((r) => new MessageTypeMetadata(r.MessageType, namesByRegistration[r]))]));
        }

        logger.LogInformation(
            "Created multi-type SQS subscriber on queue '{QueueName}' handling {MessageTypeCount} message types from {TopicCount} topics.",
            queueName,
            _registrations.Count,
            _topics.Count);
    }

    private sealed class TopicRegistration(Func<ITopicNamingConvention, string> resolveName, string filterPolicy, Type conventionType)
    {
        public Func<ITopicNamingConvention, string> ResolveName { get; } = resolveName;

        public string FilterPolicy { get; } = filterPolicy;

        public Type ConventionType { get; } = conventionType;
    }

    private interface IMessageTypeRegistration
    {
        Type MessageType { get; }

        string ResolveTypeName(JustSayingBus bus, IServiceResolver serviceResolver);

        IMessageBodySerializer CreateErasedSerializer(JustSayingBus bus, IServiceResolver serviceResolver);

        void RegisterHandler(JustSayingBus bus, IHandlerResolver handlerResolver, IServiceResolver serviceResolver, string queueName);
    }

    private sealed class MessageTypeRegistration<TMessage>(
        string typeName,
        Action<HandlerMiddlewareBuilder> middlewareConfiguration,
        Func<IServiceResolver, IMessageBodySerializer<TMessage>> serializerFactory = null,
        Func<IServiceResolver, string> typeNameResolver = null)
        : IMessageTypeRegistration where TMessage : class
    {
        public Type MessageType => typeof(TMessage);

        // Precedence: an explicit type name wins; otherwise a resolver (e.g. the configured CloudEvents
        // `type`); otherwise the type's logical name (the SNS Subject).
        public string ResolveTypeName(JustSayingBus bus, IServiceResolver serviceResolver)
            => typeName
               ?? typeNameResolver?.Invoke(serviceResolver)
               ?? bus.MessageTypeRegistry.GetLogicalName(typeof(TMessage));

        public IMessageBodySerializer CreateErasedSerializer(JustSayingBus bus, IServiceResolver serviceResolver)
            => (serializerFactory is null
                ? bus.MessageBodySerializerFactory.GetSerializer<TMessage>()
                : serializerFactory(serviceResolver)).Erase();

        public void RegisterHandler(JustSayingBus bus, IHandlerResolver handlerResolver, IServiceResolver serviceResolver, string queueName)
        {
            var resolutionContext = new HandlerResolutionContext(queueName);
            var proposedHandler = handlerResolver.ResolveHandler<TMessage>(resolutionContext)
                ?? throw new HandlerNotRegisteredWithContainerException($"There is no handler for '{typeof(TMessage).ToReadableFullName()}' messages.");

            var middleware = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver, typeof(TMessage), bus.MessageMetadataProvider)
                .Configure(middlewareConfiguration ?? (b => b.UseDefaults<TMessage>(proposedHandler.GetType())))
                .Build();

            bus.AddMessageMiddleware<TMessage>(queueName, middleware);
        }
    }
}
