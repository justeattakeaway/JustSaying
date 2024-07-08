using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
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

    /// <inheritdoc />
    public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

        logger.LogInformation("Adding SQS publisher for message type '{MessageType}'", typeof(T));

        var config = bus.Config;
        bus.SerializationRegister.AddSerializer<T>();

        var eventPublisher = new SqsMessagePublisher(
            _queueAddress.QueueUrl,
            proxy.GetAwsClientFactory().GetSqsClient(RegionEndpoint.GetBySystemName(_queueAddress.RegionName)),
            bus.SerializationRegister,
            loggerFactory)
        {
            MessageResponseLogger = config.MessageResponseLogger,
            CompressionRegistry = bus.CompressionRegistry,
            CompressionOptions = _compressionOptions ?? bus.Config.CompressionOptions
        };
        CompressionEncodingValidator.ValidateEncoding(bus.CompressionRegistry, eventPublisher.CompressionOptions);

        bus.AddMessagePublisher<T>(eventPublisher);

        logger.LogInformation(
            "Created SQS queue publisher on queue URL '{QueueName}' for message type '{MessageType}'",
            _queueAddress.QueueUrl,
            typeof(T));
    }
}
