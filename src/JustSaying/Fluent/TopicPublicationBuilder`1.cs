using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A builder for a topic publication's publish-time behaviour. The destination — which topic, and
/// (when JustSaying owns it) how to create it — is a <see cref="Topic"/> value supplied at
/// registration; this builder is the same whether the topic is created by JustSaying or already
/// exists, and exposes no infrastructure configuration. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message.
/// </typeparam>
public sealed class TopicPublicationBuilder<T> : IPublicationBuilder<T> where T : class
{
    private readonly Topic _destination;

    private string TopicName { get; set; }

    private string Subject { get; set; }

    private bool SubjectSet { get; set; }

    private PublishCompressionOptions CompressionOptions { get; set; }

    private bool IsRawMessage { get; set; }

    private ServerSideEncryption Encryption { get; set; }

    private Func<Exception, T, bool> ExceptionHandler { get; set; }

    private Func<Exception, IReadOnlyCollection<T>, bool> ExceptionBatchHandler { get; set; }

    private Action<PublishMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    /// <summary>
    /// Function that will produce a topic name dynamically from a message at publish time.
    /// If the topic doesn't exist, it will be created at that point. Only applicable when
    /// JustSaying owns the topic (not for a topic addressed by ARN).
    /// </summary>
    public Func<T, string> TopicNameCustomizer { get; set; }

    /// <summary>
    /// Function that will produce a topic ARN dynamically from a message and the registered topic
    /// ARN at publish time. Only applicable for a topic addressed by ARN.
    /// </summary>
    public Func<string, T, string> TopicAddressCustomizer { get; set; }

    /// <summary>
    /// An optional custom serializer for this publication, used instead of the per-type default from
    /// the bus's serialization factory. Built from the bus's <see cref="IServiceResolver"/> so a
    /// serializer package can resolve its own serialization services from the container without
    /// replacing the app-wide factory. Internal extensibility seam for serializer packages (such as
    /// JustSaying.CloudEvents, which exposes it via <c>WithCloudEventTopic&lt;T&gt;</c>).
    /// </summary>
    internal Func<IServiceResolver, JustSaying.Messaging.MessageSerialization.IMessageBodySerializer<T>> SerializerOverride { get; set; }

    /// <summary>
    /// An optional resolver for the SNS <c>Subject</c> stamped on published messages, applied when no
    /// explicit subject is set — instead of the logical name of <typeparamref name="T"/>. Internal
    /// extensibility seam used by wrapper publications (such as CloudEvents envelopes) so the subject
    /// reflects the payload type rather than the wrapper type.
    /// </summary>
    internal Func<JustSaying.Messaging.MessageSerialization.IMessageTypeRegistry, string> SubjectResolver { get; set; }

    /// <summary>
    /// An optional resolver for the topic name, applied when no explicit name is set — instead of the
    /// naming convention keyed on <typeparamref name="T"/>. Internal extensibility seam used by wrapper
    /// publications (such as CloudEvents envelopes) so the topic is named after the payload type rather
    /// than the wrapper type.
    /// </summary>
    internal Func<JustSaying.Naming.ITopicNamingConvention, string> TopicNameResolver { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicPublicationBuilder{T}"/> class.
    /// </summary>
    internal TopicPublicationBuilder()
        : this(Topic.ByConvention())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicPublicationBuilder{T}"/> class for the
    /// specified destination.
    /// </summary>
    /// <param name="destination">The topic the publication targets.</param>
    internal TopicPublicationBuilder(Topic destination)
    {
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    /// <summary>
    /// Configures the name of the topic. Equivalent to registering the publication against
    /// <see cref="Topic.Named(string)"/>.
    /// </summary>
    /// <param name="name">The name of the topic to publish to.</param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public TopicPublicationBuilder<T> WithTopicName(string name)
    {
        TopicName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Configures the name of the topic by calling this func at publish time to determine the name of the topic.
    /// If the topic does not exist, it will be created on first publish.
    /// </summary>
    /// <param name="topicNameCustomizer">Function that will be called at publish time to determine the name of the target topic for this <see cref="T"/>.
    /// <para>
    /// For example: <c>WithTopicName(msg => $"{msg.Tenant}-mymessage")</c> with <c>msg.Tenant</c> of <c>["uk", "au"]</c> would
    /// create topics <c>"uk-mymessage"</c> and <c>"au-mymessage"</c> when a message is published with those tenants.
    /// </para>
    /// </param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    public TopicPublicationBuilder<T> WithTopicName(Func<T, string> topicNameCustomizer)
    {
        TopicNameCustomizer = topicNameCustomizer;
        return this;
    }

    /// <summary>
    /// Configures the address of the topic by calling this function at publish time to determine the topic ARN.
    /// Only applicable for a topic addressed by ARN (<see cref="Topic.FromArn(string)"/>).
    /// </summary>
    /// <param name="topicAddressCustomizer">Function that will be called at publish time to determine the ARN of the target topic for this <see cref="T"/>.
    /// <para>
    /// For example: <c>WithTopicAddress((arn, msg) => $"arn:aws:sns:eu-west-1:00000000:{msg.Tenant}-mymessage")</c> with <c>msg.Tenant</c> of <c>["uk", "au"]</c> would
    /// publish to topics <c>"uk-mymessage"</c> and <c>"au-mymessage"</c> when a message is published with those tenants.
    /// </para>
    /// </param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    public TopicPublicationBuilder<T> WithTopicAddress(Func<string, T, string> topicAddressCustomizer)
    {
        TopicAddressCustomizer = topicAddressCustomizer;
        return this;
    }

    /// <summary>
    /// Configures the SNS <c>Subject</c> stamped on published messages, instead of the message type's
    /// logical name.
    /// </summary>
    /// <param name="subject">The subject to stamp on published messages.</param>
    /// <returns>The current <see cref="TopicPublicationBuilder{T}"/>.</returns>
    public TopicPublicationBuilder<T> WithSubject(string subject)
    {
        Subject = subject;
        SubjectSet = true;
        return this;
    }

    /// <summary>
    /// Sets the compression options for publishing messages.
    /// </summary>
    /// <param name="compressionOptions">The compression options to use when publishing messages.</param>
    /// <returns>The current <see cref="TopicPublicationBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compressionOptions"/> is <see langword="null"/>.</exception>
    public TopicPublicationBuilder<T> WithCompression(PublishCompressionOptions compressionOptions)
    {
        CompressionOptions = compressionOptions ?? throw new ArgumentNullException(nameof(compressionOptions));
        return this;
    }

    /// <summary>
    /// Publishes the message body verbatim, without JustSaying's metadata conventions.
    /// </summary>
    /// <returns>The current <see cref="TopicPublicationBuilder{T}"/>.</returns>
    public TopicPublicationBuilder<T> WithRawMessages()
    {
        IsRawMessage = true;
        return this;
    }

    /// <summary>
    /// Configures an exception handler to use when publishing a message fails.
    /// </summary>
    /// <param name="exceptionHandler">A delegate to invoke if an exception is thrown while publishing.</param>
    /// <returns>The current <see cref="TopicPublicationBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exceptionHandler"/> is <see langword="null"/>.</exception>
    public TopicPublicationBuilder<T> WithExceptionHandler(Func<Exception, T, bool> exceptionHandler)
    {
        ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        return this;
    }

    /// <summary>
    /// Configures an exception handler to use when publishing a batch of messages fails.
    /// </summary>
    /// <param name="exceptionBatchHandler">A delegate to invoke if an exception is thrown while publishing a batch.</param>
    /// <returns>The current <see cref="TopicPublicationBuilder{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exceptionBatchHandler"/> is <see langword="null"/>.</exception>
    public TopicPublicationBuilder<T> WithExceptionHandler(Func<Exception, IReadOnlyCollection<T>, bool> exceptionBatchHandler)
    {
        ExceptionBatchHandler = exceptionBatchHandler ?? throw new ArgumentNullException(nameof(exceptionBatchHandler));
        return this;
    }

    /// <summary>
    /// Configures the publish middleware pipeline for this publication.
    /// </summary>
    /// <param name="middlewareConfiguration">A delegate to configure the publish middleware pipeline.</param>
    /// <returns>The current <see cref="TopicPublicationBuilder{T}"/>.</returns>
    public TopicPublicationBuilder<T> WithMiddlewareConfiguration(
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
        var logger = loggerFactory.CreateLogger<TopicPublicationBuilder<T>>();

        logger.LogInformation("Adding SNS publisher for message type '{MessageType}'.",
            typeof(T));

        _serviceResolver = serviceResolver;

        if (_destination.IsAddress)
        {
            ConfigureForAddress(bus, proxy, loggerFactory);
        }
        else
        {
            ConfigureForOwnedTopic(bus, proxy, loggerFactory);
        }

        if (MiddlewareConfiguration != null)
        {
            var middlewareBuilder = new PublishMiddlewareBuilder(serviceResolver);
            middlewareBuilder.Configure(MiddlewareConfiguration);
            bus.AddPublishMiddleware<T>(middlewareBuilder.Build());
        }
    }

    private void ConfigureForOwnedTopic(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        if (TopicAddressCustomizer is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(WithTopicAddress)} only applies to a topic addressed by ARN; use {nameof(Topic)}.{nameof(Topic.FromArn)}(...) or {nameof(WithTopicName)}.");
        }

        if (TopicName is not null && _destination.Name is not null)
        {
            throw new InvalidOperationException(
                $"The topic is named both by the {nameof(Topic)} destination ('{_destination.Name}') and {nameof(WithTopicName)} ('{TopicName}'); name it once.");
        }

        var region = bus.Config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(bus.Config.Region)} property.");

        var writeConfiguration = new SnsWriteConfiguration
        {
            Encryption = _destination.Infrastructure?.Encryption,
            CompressionOptions = CompressionOptions ?? bus.Config.DefaultCompressionOptions,
            IsRawMessage = IsRawMessage,
        };

        if (SubjectSet)
        {
            writeConfiguration.Subject = Subject;
        }

        CompressionEncodingValidator.ValidateEncoding(bus.CompressionRegistry, writeConfiguration.CompressionOptions);

        var client = proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region));

        Func<Exception, object, bool> exceptionHandler =
            ExceptionHandler is null ? null : (ex, message) => ExceptionHandler(ex, (T)message);
        Func<Exception, IReadOnlyCollection<object>, bool> exceptionBatchHandler =
            ExceptionBatchHandler is null ? null : (ex, messages) => ExceptionBatchHandler(ex, messages.Cast<T>().ToList());

        StaticPublicationConfiguration BuildConfiguration(string topicName)
            => StaticPublicationConfiguration.Build<T>(topicName,
                _destination.Infrastructure?.Tags ?? new Dictionary<string, string>(StringComparer.Ordinal),
                writeConfiguration,
                client,
                loggerFactory,
                bus,
                exceptionHandler,
                exceptionBatchHandler,
                serviceResolver: _serviceResolver,
                serializerFactory: SerializerOverride,
                subjectResolver: SubjectResolver);

        var topicName = TopicName ?? _destination.Name;
        if (string.IsNullOrEmpty(topicName) && TopicNameResolver is not null)
        {
            topicName = TopicNameResolver(bus.Config.TopicNamingConvention);
        }

        ITopicPublisher config = TopicNameCustomizer != null
            ? DynamicPublicationConfiguration.Build<T>(message => TopicNameCustomizer((T)message), BuildConfiguration, loggerFactory)
            : BuildConfiguration(topicName ?? string.Empty);

        bus.AddStartupTask(config.StartupTask);
        bus.AddMessagePublisher<T>(config.Publisher);
        bus.AddMessageBatchPublisher<T>(config.BatchPublisher);
    }

    private void ConfigureForAddress(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        if (TopicNameCustomizer is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(WithTopicName)} customization only applies to a topic JustSaying owns; use {nameof(WithTopicAddress)} for a topic addressed by ARN.");
        }

        if (TopicName is not null)
        {
            throw new InvalidOperationException(
                $"A topic addressed by ARN cannot also be named; remove the {nameof(WithTopicName)} call.");
        }

        var arn = Arn.Parse(_destination.Address.TopicArn);

        var compressionRegistry = bus.CompressionRegistry;
        var compressionOptions = CompressionOptions ?? bus.Config.DefaultCompressionOptions;
        var serializer = SerializerOverride is null
            ? bus.MessageBodySerializerFactory.GetSerializer<T>()
            : SerializerOverride(_serviceResolver);
        var subject = SubjectSet
            ? Subject
            : SubjectResolver?.Invoke(bus.MessageTypeRegistry) ?? bus.MessageTypeRegistry.GetLogicalName(typeof(T));

        CompressionEncodingValidator.ValidateEncoding(bus.CompressionRegistry, compressionOptions);

        Func<Exception, object, bool> exceptionHandler =
            ExceptionHandler is null ? null : (ex, message) => ExceptionHandler(ex, (T)message);
        Func<Exception, IReadOnlyCollection<object>, bool> exceptionBatchHandler =
            ExceptionBatchHandler is null ? null : (ex, messages) => ExceptionBatchHandler(ex, messages.Cast<T>().ToList());

        StaticAddressPublicationConfiguration BuildConfiguration(string topicArn)
            => StaticAddressPublicationConfiguration.Build<T>(
                topicArn,
                proxy.GetAwsClientFactory(),
                new OutboundMessageConverter(PublishDestinationType.Topic, serializer.Erase(), compressionRegistry, compressionOptions, subject, true),
                loggerFactory,
                bus,
                exceptionHandler,
                exceptionBatchHandler);

        ITopicAddressPublisher publisherConfig = TopicAddressCustomizer != null
            ? DynamicAddressPublicationConfiguration.Build<T>(_destination.Address.TopicArn, (topicArn, message) => TopicAddressCustomizer(topicArn, (T)message), BuildConfiguration, loggerFactory)
            : BuildConfiguration(_destination.Address.TopicArn);

        bus.AddMessagePublisher<T>(publisherConfig.Publisher);
        bus.AddMessageBatchPublisher<T>(publisherConfig.BatchPublisher);

        loggerFactory.CreateLogger<TopicPublicationBuilder<T>>().LogInformation(
            "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'",
            arn.Resource,
            typeof(T));
    }

    // Stashed by Configure so the per-mode helpers can build serializers from the container.
    private IServiceResolver _serviceResolver;
}
