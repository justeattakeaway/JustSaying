using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
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
    /// Sets the message to be published as raw message.
    /// </summary>
    /// <returns>The current instance of <see cref="QueueAddressPublicationBuilder{T}"/> for method chaining.</returns>
    public QueueAddressPublicationBuilder<T> WithRawMessages()
    {
        _isRawMessage = true;
        return this;
    }

    /// <inheritdoc />
    public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

        logger.LogInformation("Adding SQS publisher for message type '{MessageType}'", typeof(T));

        var config = bus.Config;
        var compressionOptions = _compressionOptions ?? bus.Config.DefaultCompressionOptions;
        var subjectProvider = bus.Config.MessageSubjectProvider;
        var subject = _subjectSet ? _subject : subjectProvider.GetSubjectForType(typeof(T));

        var eventPublisher = new SqsMessagePublisher(
            _queueAddress.QueueUrl,
            proxy.GetAwsClientFactory().GetSqsClient(RegionEndpoint.GetBySystemName(_queueAddress.RegionName)),
            new PublishMessageConverter(PublishDestinationType.Queue, bus.MessageBodySerializerFactory.GetSerializer<T>(), new MessageCompressionRegistry([new GzipMessageBodyCompression()]), compressionOptions, subject, _isRawMessage),
            loggerFactory)
        {
            MessageResponseLogger = config.MessageResponseLogger
        };
        CompressionEncodingValidator.ValidateEncoding(bus.CompressionRegistry, compressionOptions);

        bus.AddMessagePublisher<T>(eventPublisher);

        logger.LogInformation(
            "Created SQS queue publisher on queue URL '{QueueName}' for message type '{MessageType}'",
            _queueAddress.QueueUrl,
            typeof(T));
    }
}
