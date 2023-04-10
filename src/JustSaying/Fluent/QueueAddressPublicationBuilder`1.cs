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
    where T : class
{
    private readonly QueueAddress _queueAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueAddressPublicationBuilder{T}"/> class.
    /// </summary>
    /// <param name="queueAddress">The address of the queue to publish to.</param>
    internal QueueAddressPublicationBuilder(QueueAddress queueAddress)
    {
        _queueAddress = queueAddress;
    }

    /// <inheritdoc />
    public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

        logger.LogInformation("Adding SQS publisher for message type '{MessageType}'", typeof(T));

        bus.SerializationRegister.AddSerializer<T>();

        var eventPublisher = new SqsMessagePublisher<T>(
            _queueAddress.QueueUrl,
            proxy.GetAwsClientFactory().GetSqsClient(RegionEndpoint.GetBySystemName(_queueAddress.RegionName)),
            bus.SerializationRegister,
            loggerFactory);

        bus.AddMessagePublisher<T>(eventPublisher);

        logger.LogInformation(
            "Created SQS queue publisher on queue URL '{QueueName}' for message type '{MessageType}'",
            _queueAddress.QueueUrl,
            typeof(T));
    }
}
