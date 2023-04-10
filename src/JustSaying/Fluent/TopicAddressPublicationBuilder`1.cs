using Amazon;
using JustSaying.AwsTools;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a topic publication to an existing topic. This class cannot be inherited.
/// </summary>
/// <typeparam name="TMessage">
/// The type of the message.
/// </typeparam>
public sealed class TopicAddressPublicationBuilder<TMessage> : IPublicationBuilder
    where TMessage : class
{
    private readonly TopicAddress _topicAddress;
    private Func<Exception,TMessage,bool> _exceptionHandler;

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
    public TopicAddressPublicationBuilder<TMessage> WithExceptionHandler(Func<Exception, TMessage, bool> exceptionHandler)
    {
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        return this;
    }

    /// <inheritdoc />
    public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<TMessage>>();

        logger.LogInformation("Adding SNS publisher for message type '{MessageType}'", typeof(TMessage));

        var config = bus.Config;
        var arn = Arn.Parse(_topicAddress.TopicArn);

        bus.SerializationRegister.AddSerializer<TMessage>();

        var eventPublisher = new TopicAddressPublisher<TMessage>(
            proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(arn.Region)),
            loggerFactory,
            config.MessageSubjectProvider,
            bus.SerializationRegister,
            _exceptionHandler,
            _topicAddress);
        bus.AddMessagePublisher<TMessage>(eventPublisher);

        logger.LogInformation(
            "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'",
            arn.Resource,
            typeof(TMessage));
    }
}
