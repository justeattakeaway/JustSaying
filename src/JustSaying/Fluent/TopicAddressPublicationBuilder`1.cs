using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a topic publication to an existing topic. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message.
/// </typeparam>
public sealed class TopicAddressPublicationBuilder<T> : IPublicationBuilder<T>
    where T : Message
{
    private readonly TopicAddress _topicAddress;
    private Func<Exception,Message,bool> _exceptionHandler;
    private Func<Exception, IReadOnlyCollection<Message>, bool> _exceptionBatchHandler;

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
    public TopicAddressPublicationBuilder<T> WithExceptionHandler(Func<Exception, Message, bool> exceptionHandler)
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
    public TopicAddressPublicationBuilder<T> WithExceptionHandler(Func<Exception, IReadOnlyCollection<Message>, bool> exceptionBatchHandler)
    {
        _exceptionBatchHandler = exceptionBatchHandler ?? throw new ArgumentNullException(nameof(exceptionBatchHandler));
        return this;
    }

    /// <inheritdoc />
    public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

        logger.LogInformation("Adding SNS publisher for message type '{MessageType}'", typeof(T));

        var config = bus.Config;
        var arn = Arn.Parse(_topicAddress.TopicArn);

        bus.SerializationRegister.AddSerializer<T>();

        var eventPublisher = new SnsMessagePublisher(
            _topicAddress.TopicArn,
            proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(arn.Region)),
            bus.SerializationRegister,
            loggerFactory,
            config.MessageSubjectProvider,
            _exceptionHandler)
        {
            HandleBatchException = _exceptionBatchHandler,
        };

        bus.AddMessagePublisher<T>(eventPublisher);

        logger.LogInformation(
            "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'",
            arn.Resource,
            typeof(T));
    }
}
