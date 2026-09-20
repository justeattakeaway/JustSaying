using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a queue publication to an existing queue. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message published to the queue.
/// </typeparam>
public sealed class QueueAddressPublicationBuilder<T> : IPublicationBuilder<T>
    where T : Message
{
    private readonly QueueAddress _queueAddress;
    private PublishCompressionOptions _compressionOptions;
    private string _subject;
    private bool _subjectSet;
    private bool _isRawMessage;
    private bool _shouldCheckQueueExistence;
    private int? _maximumMessageSize;

    private Action<PublishMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueAddressPublicationBuilder{T}"/> class.
    /// </summary>
    /// <param name="queueAddress">The address of the queue to publish to.</param>
    internal QueueAddressPublicationBuilder(QueueAddress queueAddress)
    {
        _queueAddress = queueAddress;
    }

    /// <summary>
    /// Sets the compression options for publishing messages.
    /// </summary>
    /// <param name="compressionOptions">The compression options to use when publishing messages.</param>
    /// <returns>The current instance of <see cref="QueueAddressPublicationBuilder{T}"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="compressionOptions"/> is null.</exception>
    public QueueAddressPublicationBuilder<T> WithCompression(PublishCompressionOptions compressionOptions)
    {
        _compressionOptions = compressionOptions ?? throw new ArgumentNullException(nameof(compressionOptions));
        return this;
    }

    /// <summary>
    /// Sets the subject for the message.
    /// </summary>
    /// <param name="subject">The subject to set for the message.</param>
    /// <returns>The current instance of <see cref="QueueAddressPublicationBuilder{T}"/> for method chaining.</returns>
    public QueueAddressPublicationBuilder<T> WithSubject(string subject)
    {
        _subject = subject;
        _subjectSet = true;
        return this;
    }

    /// <summary>
    /// Sets the message to be published as a raw message.
    /// </summary>
    /// <returns>The current instance of <see cref="QueueAddressPublicationBuilder{T}"/> for method chaining.</returns>
    public QueueAddressPublicationBuilder<T> WithRawMessages()
    {
        _isRawMessage = true;
        return this;
    }

    /// <summary>
    /// Sets the maximum size, in bytes, of a message the queue will accept.
    /// </summary>
    /// <param name="maximumMessageSize">The maximum message size, in bytes.</param>
    /// <returns>The current instance of <see cref="QueueAddressPublicationBuilder{T}"/> for method chaining.</returns>
    /// <remarks>
    /// JustSaying does not create or configure a queue it is given the address of, so tell it here if the queue
    /// has had its <c>MaximumMessageSize</c> attribute set below the SQS default of 1 MiB, which is common for
    /// queues created by infrastructure tooling that still defaults to 256 KiB. The value is used as the budget
    /// for compression. It does not affect batching, SQS allows a batch to add up to 1 MiB whatever the queue's limit.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maximumMessageSize"/> is outside the range SQS accepts.
    /// </exception>
    public QueueAddressPublicationBuilder<T> WithMaximumMessageSize(int maximumMessageSize)
    {
        if (maximumMessageSize < JustSayingConstants.MinimumSqsMessageSize ||
            maximumMessageSize > JustSayingConstants.MaximumSqsMessageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMessageSize),
                maximumMessageSize,
                $"The maximum message size must be between {JustSayingConstants.MinimumSqsMessageSize} and {JustSayingConstants.MaximumSqsMessageSize} bytes.");
        }

        _maximumMessageSize = maximumMessageSize;
        return this;
    }

    /// <summary>
    /// Checks that the configured SQS queue exists before the bus starts publishing messages.
    /// </summary>
    /// <returns>
    /// The current <see cref="QueueAddressPublicationBuilder{T}"/>.
    /// </returns>
    public QueueAddressPublicationBuilder<T> WithQueueExistenceCheck()
    {
        _shouldCheckQueueExistence = true;
        return this;
    }

    /// <summary>
    /// Configures the publish middleware pipeline for this publication.
    /// </summary>
    /// <param name="middlewareConfiguration">A delegate to configure the publish middleware pipeline.</param>
    /// <returns>The current <see cref="QueueAddressPublicationBuilder{T}"/>.</returns>
    public QueueAddressPublicationBuilder<T> WithMiddlewareConfiguration(
        Action<PublishMiddlewareBuilder> middlewareConfiguration)
    {
        MiddlewareConfiguration = middlewareConfiguration;
        return this;
    }

    /// <inheritdoc />
    void IPublicationBuilder<T>.Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory, IServiceResolver serviceResolver)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

        logger.LogInformation("Adding SQS publisher for message type '{MessageType}'", typeof(T));

        var config = bus.Config;
        var compressionOptions = _compressionOptions ?? bus.Config.DefaultCompressionOptions;
        var subjectProvider = bus.Config.MessageSubjectProvider;
        var subject = _subjectSet ? _subject : subjectProvider.GetSubjectForType(typeof(T));
        var sqsClient = proxy.GetAwsClientFactory().GetSqsClient(RegionEndpoint.GetBySystemName(_queueAddress.RegionName));

        if (_shouldCheckQueueExistence)
        {
            var queue = new QueueAddressQueue(_queueAddress, sqsClient);
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
            _queueAddress.QueueUrl,
            sqsClient,
            new OutboundMessageConverter(PublishDestinationType.Queue, bus.MessageBodySerializerFactory.GetSerializer<T>(), bus.CompressionRegistry, compressionOptions, subject, _isRawMessage, _maximumMessageSize ?? JustSayingConstants.DefaultSqsMaximumMessageSize),
            loggerFactory)
        {
            MessageResponseLogger = config.MessageResponseLogger
        };
        CompressionEncodingValidator.ValidateEncoding(bus.CompressionRegistry, compressionOptions);

        bus.AddMessagePublisher<T>(eventPublisher);

        if (MiddlewareConfiguration != null)
        {
            var middlewareBuilder = new PublishMiddlewareBuilder(serviceResolver);
            middlewareBuilder.Configure(MiddlewareConfiguration);
            bus.AddPublishMiddleware<T>(middlewareBuilder.Build());
        }

        logger.LogInformation(
            "Created SQS queue publisher on queue URL '{QueueName}' for message type '{MessageType}'",
            _queueAddress.QueueUrl,
            typeof(T));
    }
}
