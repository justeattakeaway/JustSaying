using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a topic publication to an existing topic. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message.
/// </typeparam>
public sealed class TopicAddressPublicationBuilder<T> : IPublicationBuilder<T> where T : class
{
    private readonly TopicAddress _topicAddress;
    private Func<Exception, T, bool> _exceptionHandler;
    private Func<Exception, IReadOnlyCollection<T>, bool> _exceptionBatchHandler;
    private PublishCompressionOptions _compressionOptions;
    private string _subject;
    private bool _subjectSet;

    /// <summary>
    /// Function that will produce a topic address dynamically from a message and the original topic
    /// address at publish time.
    /// </summary>
    public Func<string, T, string> TopicAddressCustomizer { get; set; }

    private Action<PublishMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicAddressPublicationBuilder{T}"/> class.
    /// </summary>
    /// <param name="topicAddress">The address of the topic to publish to.</param>
    internal TopicAddressPublicationBuilder(TopicAddress topicAddress)
    {
        _topicAddress = topicAddress;
    }

    /// <summary>
    /// Configures an exception handler to use.
    /// </summary>
    /// <param name="exceptionHandler">A delegate to invoke if an exception is thrown while publishing.</param>
    /// <returns>
    /// The current <see cref="TopicAddressPublicationBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="exceptionHandler"/> is <see langword="null"/>.
    /// </exception>
    public TopicAddressPublicationBuilder<T> WithExceptionHandler(Func<Exception, T, bool> exceptionHandler)
    {
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        return this;
    }

    /// <summary>
    /// Configures an exception handler to use.
    /// </summary>
    /// <param name="exceptionBatchHandler">A delegate to invoke if an exception is thrown while publishing a batch.</param>
    /// <returns>
    /// The current <see cref="TopicAddressPublicationBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="exceptionBatchHandler"/> is <see langword="null"/>.
    /// </exception>
    public TopicAddressPublicationBuilder<T> WithExceptionHandler(Func<Exception, IReadOnlyCollection<T>, bool> exceptionBatchHandler)
    {
        _exceptionBatchHandler = exceptionBatchHandler ?? throw new ArgumentNullException(nameof(exceptionBatchHandler));
        return this;
    }

    /// <summary>
    /// Sets the compression options for publishing messages.
    /// </summary>
    /// <param name="compressionOptions">The compression options to use when publishing messages.</param>
    /// <returns>The current instance of <see cref="TopicAddressPublicationBuilder{T}"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressionOptions"/> is null.</exception>
    public TopicAddressPublicationBuilder<T> WithCompression(PublishCompressionOptions compressionOptions)
    {
        _compressionOptions = compressionOptions ?? throw new ArgumentNullException(nameof(compressionOptions));
        return this;
    }

    public TopicAddressPublicationBuilder<T> WithSubject(string subject)
    {
        _subject = subject;
        _subjectSet = true;
        return this;
    }

    /// <summary>
    /// Configures the address of the topic by calling this function at publish time to determine the topic ARN.
    /// </summary>
    /// <param name="topicAddressCustomizer">Function that will be called at publish time to determine the ARN of the target topic for this <see cref="T"/>.
    /// <para>
    /// For example: <c>WithTopicAddress(msg => $"arn:aws:sns:eu-west-1:00000000:{msg.Tenant}-mymessage")</c> with <c>msg.Tenant</c> of <c>["uk", "au"]</c> would
    /// publish to topics <c>"uk-mymessage"</c> and <c>"au-mymessage"</c> when a message is published with those tenants.
    /// </para>
    /// </param>
    /// <returns>
    /// The current <see cref="TopicAddressPublicationBuilder{T}"/>.
    /// </returns>
    public TopicAddressPublicationBuilder<T> WithTopicAddress(Func<string, T, string> topicAddressCustomizer)
    {
        TopicAddressCustomizer = topicAddressCustomizer;
        return this;
    }

    /// <summary>
    /// Configures the publish middleware pipeline for this publication.
    /// </summary>
    /// <param name="middlewareConfiguration">A delegate to configure the publish middleware pipeline.</param>
    /// <returns>The current <see cref="TopicAddressPublicationBuilder{T}"/>.</returns>
    public TopicAddressPublicationBuilder<T> WithMiddlewareConfiguration(
        Action<PublishMiddlewareBuilder> middlewareConfiguration)
    {
        MiddlewareConfiguration = middlewareConfiguration;
        return this;
    }

    /// <inheritdoc />
    void IPublicationBuilder<T>.Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory, IServiceResolver serviceResolver)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

        logger.LogInformation("Adding SNS publisher for message type '{MessageType}'", typeof(T));

        var arn = Arn.Parse(_topicAddress.TopicArn);

        var compressionRegistry = bus.CompressionRegistry;
        var compressionOptions = _compressionOptions ?? bus.Config.DefaultCompressionOptions;
        var serializer = bus.MessageBodySerializerFactory.GetSerializer<T>();
        var subject = _subjectSet ? _subject : bus.MessageTypeRegistry.GetLogicalName(typeof(T));

        CompressionEncodingValidator.ValidateEncoding(bus.CompressionRegistry, compressionOptions);

        Func<Exception, object, bool> exceptionHandler =
            _exceptionHandler is null ? null : (ex, message) => _exceptionHandler(ex, (T)message);
        Func<Exception, IReadOnlyCollection<object>, bool> exceptionBatchHandler =
            _exceptionBatchHandler is null ? null : (ex, messages) => _exceptionBatchHandler(ex, messages.Cast<T>().ToList());

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
            ? DynamicAddressPublicationConfiguration.Build<T>(_topicAddress.TopicArn, (topicArn, message) => TopicAddressCustomizer(topicArn, (T)message), BuildConfiguration, loggerFactory)
            : BuildConfiguration(_topicAddress.TopicArn);

        bus.AddMessagePublisher<T>(publisherConfig.Publisher);
        bus.AddMessageBatchPublisher<T>(publisherConfig.BatchPublisher);

        if (MiddlewareConfiguration != null)
        {
            var middlewareBuilder = new PublishMiddlewareBuilder(serviceResolver);
            middlewareBuilder.Configure(MiddlewareConfiguration);
            bus.AddPublishMiddleware<T>(middlewareBuilder.Build());
        }

        logger.LogInformation(
            "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'",
            arn.Resource,
            typeof(T));
    }
}
