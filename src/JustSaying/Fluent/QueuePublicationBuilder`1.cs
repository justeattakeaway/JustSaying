using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Metadata;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A builder for a queue publication's publish-time behaviour. The destination — which queue, and
/// (when JustSaying owns it) how to create it — is a <see cref="QueueDestination"/> value supplied at
/// registration; this builder is the same whether the queue is created by JustSaying or already
/// exists, and exposes no infrastructure configuration. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message published to the queue.
/// </typeparam>
public sealed class QueuePublicationBuilder<T> : IPublicationBuilder<T> where T : class
{
    private readonly QueueDestination _destination;

    private string QueueName { get; set; }

    private string Subject { get; set; }

    private bool SubjectSet { get; set; }

    private PublishCompressionOptions CompressionOptions { get; set; }

    private bool _isRawMessage;

    private bool _shouldCheckQueueExistence;

    private Action<PublishMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    /// <summary>
    /// An optional custom serializer for this publication, used instead of the per-type default from
    /// the bus's serialization factory. Built from the bus's <see cref="IServiceResolver"/> so a
    /// serializer package can resolve its own serialization services from the container without
    /// replacing the app-wide factory. Internal extensibility seam for serializer packages (such as
    /// JustSaying.CloudEvents, which exposes it via <c>WithCloudEventQueue&lt;T&gt;</c>).
    /// </summary>
    internal Func<IServiceResolver, IMessageBodySerializer<T>> SerializerOverride { get; set; }

    /// <summary>
    /// An optional resolver for the <c>Subject</c> written into the queue envelope, used instead of the
    /// logical name of <typeparamref name="T"/>. Internal extensibility seam used by wrapper
    /// publications so the subject reflects the payload type rather than the wrapper type.
    /// </summary>
    internal Func<IMessageTypeRegistry, string> SubjectResolver { get; set; }

    /// <summary>
    /// An optional resolver for the queue name, applied when no explicit name is set — instead of the
    /// naming convention keyed on <typeparamref name="T"/>. Internal extensibility seam used by wrapper
    /// publications (such as CloudEvents envelopes) so the queue is named after the payload type rather
    /// than the wrapper type.
    /// </summary>
    internal Func<JustSaying.Naming.IQueueNamingConvention, string> QueueNameResolver { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueuePublicationBuilder{T}"/> class.
    /// </summary>
    internal QueuePublicationBuilder()
        : this(QueueDestination.ByConvention())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueuePublicationBuilder{T}"/> class for the
    /// specified destination.
    /// </summary>
    /// <param name="destination">The queue the publication targets.</param>
    internal QueuePublicationBuilder(QueueDestination destination)
    {
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    /// <summary>
    /// Configures the SQS queue name, rather than using the naming convention. Equivalent to
    /// registering the publication against <see cref="QueueDestination.Named(string)"/>.
    /// </summary>
    /// <param name="name">The name of the queue to publish to.</param>
    /// <returns>
    /// The current <see cref="QueuePublicationBuilder{T}"/>.
    /// </returns>
    public QueuePublicationBuilder<T> WithQueueName(string name)
    {
        QueueName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Configures the <c>Subject</c> written into the queue envelope, instead of the message type's
    /// logical name.
    /// </summary>
    /// <param name="subject">The subject to write.</param>
    /// <returns>The current <see cref="QueuePublicationBuilder{T}"/>.</returns>
    public QueuePublicationBuilder<T> WithSubject(string subject)
    {
        Subject = subject;
        SubjectSet = true;
        return this;
    }

    /// <summary>
    /// Sets the compression options for publishing messages.
    /// </summary>
    /// <param name="compressionOptions">The compression options to use when publishing messages.</param>
    /// <returns>The current <see cref="QueuePublicationBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compressionOptions"/> is <see langword="null"/>.</exception>
    public QueuePublicationBuilder<T> WithCompression(PublishCompressionOptions compressionOptions)
    {
        CompressionOptions = compressionOptions ?? throw new ArgumentNullException(nameof(compressionOptions));
        return this;
    }

    /// <summary>
    /// Publishes the message body to the queue verbatim, without JustSaying's
    /// <c>{ "Message", "Subject" }</c> queue envelope.
    /// </summary>
    /// <returns>
    /// The current <see cref="QueuePublicationBuilder{T}"/>.
    /// </returns>
    public QueuePublicationBuilder<T> WithRawMessages()
    {
        _isRawMessage = true;
        return this;
    }

    /// <summary>
    /// Checks that the configured SQS queue exists before the bus starts publishing messages. Only
    /// applicable for a pre-existing queue (a queue JustSaying owns is created on startup).
    /// </summary>
    /// <returns>
    /// The current <see cref="QueuePublicationBuilder{T}"/>.
    /// </returns>
    public QueuePublicationBuilder<T> WithQueueExistenceCheck()
    {
        _shouldCheckQueueExistence = true;
        return this;
    }

    /// <summary>
    /// Configures the publish middleware pipeline for this publication.
    /// </summary>
    /// <param name="middlewareConfiguration">A delegate to configure the publish middleware pipeline.</param>
    /// <returns>The current <see cref="QueuePublicationBuilder{T}"/>.</returns>
    public QueuePublicationBuilder<T> WithMiddlewareConfiguration(
        Action<PublishMiddlewareBuilder> middlewareConfiguration)
    {
        MiddlewareConfiguration = middlewareConfiguration;
        return this;
    }

    /// <inheritdoc />
    void IPublicationBuilder<T>.Configure(
        JustSayingBus bus,
        IAwsClientFactoryProxy proxy,
        ILoggerFactory loggerFactory,
        IServiceResolver serviceResolver)
    {
        var logger = loggerFactory.CreateLogger<QueuePublicationBuilder<T>>();

        logger.LogInformation("Adding SQS publisher for message type '{MessageType}'.",
            typeof(T));

        var config = bus.Config;
        var compressionRegistry = bus.CompressionRegistry;
        var compressionOptions = CompressionOptions ?? config.DefaultCompressionOptions;
        CompressionEncodingValidator.ValidateEncoding(compressionRegistry, compressionOptions);

        var subject = SubjectSet
            ? Subject
            : SubjectResolver?.Invoke(bus.MessageTypeRegistry) ?? bus.MessageTypeRegistry.GetLogicalName(typeof(T));

        var serializer = SerializerOverride is null
            ? bus.MessageBodySerializerFactory.GetSerializer<T>()
            : SerializerOverride(serviceResolver);
        // A self-describing serializer (for example CloudEvents) already carries the message's type
        // metadata, so the {Message, Subject} queue envelope would just double-wrap it.
        var isSelfDescribing = serializer is ISelfDescribingMessageBodySerializer;
        var isRawMessage = _isRawMessage || isSelfDescribing;

        if (isSelfDescribing && !_isRawMessage)
        {
            logger.LogInformation(
                "Publishing '{MessageType}' to the queue without the queue envelope because its serializer is self-describing.",
                typeof(T));
        }

        var metadataRegistry = serviceResolver.ResolveOptionalService<IMessagingMetadataRegistry>();

        if (_destination.IsAddress)
        {
            ConfigureForAddress(bus, proxy, loggerFactory, logger, compressionRegistry, compressionOptions, subject, serializer, isRawMessage, metadataRegistry);
        }
        else
        {
            ConfigureForOwnedQueue(bus, proxy, loggerFactory, logger, compressionRegistry, compressionOptions, subject, serializer, isRawMessage, metadataRegistry);
        }

        if (MiddlewareConfiguration != null)
        {
            var middlewareBuilder = new PublishMiddlewareBuilder(serviceResolver);
            middlewareBuilder.Configure(MiddlewareConfiguration);
            bus.AddPublishMiddleware<T>(middlewareBuilder.Build());
        }
    }

    private void ConfigureForOwnedQueue(
        JustSayingBus bus,
        IAwsClientFactoryProxy proxy,
        ILoggerFactory loggerFactory,
        ILogger logger,
        Messaging.Compression.MessageCompressionRegistry compressionRegistry,
        PublishCompressionOptions compressionOptions,
        string subject,
        IMessageBodySerializer<T> serializer,
        bool isRawMessage,
        IMessagingMetadataRegistry metadataRegistry)
    {
        if (_shouldCheckQueueExistence)
        {
            throw new InvalidOperationException(
                $"{nameof(WithQueueExistenceCheck)} only applies to a pre-existing queue; a queue JustSaying owns is created on startup.");
        }

        if (QueueName is not null && _destination.Name is not null)
        {
            throw new InvalidOperationException(
                $"The queue is named both by the {nameof(QueueDestination)} destination ('{_destination.Name}') and {nameof(WithQueueName)} ('{QueueName}'); name it once.");
        }

        var config = bus.Config;
        var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

        var writeConfiguration = new SqsWriteConfiguration
        {
            QueueName = QueueName ?? _destination.Name ?? string.Empty,
        };

        _destination.Infrastructure?.Apply(writeConfiguration);

        if (string.IsNullOrEmpty(writeConfiguration.QueueName) && QueueNameResolver is not null)
        {
            writeConfiguration.QueueName = QueueNameResolver(config.QueueNamingConvention);
        }
        writeConfiguration.ApplyQueueNamingConvention<T>(config.QueueNamingConvention);

        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        var sqsClient = proxy.GetAwsClientFactory().GetSqsClient(regionEndpoint);

        var eventPublisher = new SqsMessagePublisher(
            sqsClient,
            new OutboundMessageConverter(PublishDestinationType.Queue, serializer.Erase(), compressionRegistry, compressionOptions, subject, isRawMessage),
            loggerFactory,
            bus.Config.MessageMetadataProvider)
        {
            MessageResponseLogger = config.MessageResponseLogger,
            MessageBatchResponseLogger = bus.PublishBatchConfiguration?.MessageBatchResponseLogger
        };

#pragma warning disable 618
        var sqsQueue = new SqsQueueByName(
            regionEndpoint,
            writeConfiguration.QueueName,
            sqsClient,
            writeConfiguration.RetryCountBeforeSendingToErrorQueue,
            loggerFactory);
#pragma warning restore 618

        var tags = _destination.Infrastructure?.Tags;

        async Task StartupTask(CancellationToken cancellationToken)
        {
            if (!await sqsQueue.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                await sqsQueue.CreateAsync(writeConfiguration, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (tags is { Count: > 0 })
            {
                await sqsQueue.TagQueueAsync(sqsQueue.Uri.ToString(), tags, cancellationToken).ConfigureAwait(false);
            }

            eventPublisher.QueueUrl = sqsQueue.Uri;
        }

        bus.AddStartupTask(StartupTask);

        bus.AddMessagePublisher<T>(eventPublisher);

        if (metadataRegistry != null)
        {
            metadataRegistry.SetRegion(region);
            metadataRegistry.AddPublication(new PublicationMetadata(
                MessagingDestinationKind.SqsQueue,
                writeConfiguration.QueueName,
                isDynamic: false,
                [new MessageTypeMetadata(typeof(T), subject, serializer)],
                usesQueueEnvelope: !isRawMessage));
        }

        logger.LogInformation(
            "Created SQS publisher for message type '{MessageType}' on queue '{QueueName}'.",
            typeof(T),
            writeConfiguration.QueueName);
    }

    private void ConfigureForAddress(
        JustSayingBus bus,
        IAwsClientFactoryProxy proxy,
        ILoggerFactory loggerFactory,
        ILogger logger,
        Messaging.Compression.MessageCompressionRegistry compressionRegistry,
        PublishCompressionOptions compressionOptions,
        string subject,
        IMessageBodySerializer<T> serializer,
        bool isRawMessage,
        IMessagingMetadataRegistry metadataRegistry)
    {
        if (QueueName is not null)
        {
            throw new InvalidOperationException(
                $"A queue addressed by URL or ARN cannot also be named; remove the {nameof(WithQueueName)} call.");
        }

        var queueAddress = _destination.Address;
        var sqsClient = proxy.GetAwsClientFactory().GetSqsClient(RegionEndpoint.GetBySystemName(queueAddress.RegionName));

        if (_shouldCheckQueueExistence)
        {
            var queue = new QueueAddressQueue(queueAddress, sqsClient);
            bus.AddStartupTask(async cancellationToken =>
            {
                if (!await queue.ExistsAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException(
                        $"SQS queue '{queue.QueueName}' with URL '{queue.Uri}' does not exist.");
                }
            });
        }

        var eventPublisher = new SqsMessagePublisher(
            queueAddress.QueueUrl,
            sqsClient,
            new OutboundMessageConverter(PublishDestinationType.Queue, serializer.Erase(), compressionRegistry, compressionOptions, subject, isRawMessage),
            loggerFactory)
        {
            MessageResponseLogger = bus.Config.MessageResponseLogger
        };

        bus.AddMessagePublisher<T>(eventPublisher);

        if (metadataRegistry != null)
        {
            // The queue's region comes from its address and may differ from the bus's configured
            // region, so it is captured on the publication rather than as the registry default.
            metadataRegistry.AddPublication(new PublicationMetadata(
                MessagingDestinationKind.SqsQueue,
                queueAddress.QueueUrl.Segments[queueAddress.QueueUrl.Segments.Length - 1].TrimEnd('/'),
                isDynamic: false,
                [new MessageTypeMetadata(typeof(T), subject, serializer)],
                queueAddress.RegionName,
                usesQueueEnvelope: !isRawMessage));
        }

        logger.LogInformation(
            "Created SQS queue publisher on queue URL '{QueueName}' for message type '{MessageType}'",
            queueAddress.QueueUrl,
            typeof(T));
    }
}
