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
    private Func<Exception, Message, bool> _exceptionHandler;
    private Func<Exception, IReadOnlyCollection<Message>, bool> _exceptionBatchHandler;

    /// <summary>
    /// Function that will produce a topic address dynamically from a Message and the original topic
    /// address at publish time.
    /// </summary>
    public Func<Message, string, string> TopicAddressCustomizer { get; set; }

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

    /// <summary>
    /// Configures the address of the topic by calling this func at publish time to determine the name of the topic.
    /// </summary>
    /// <param name="topicAddressCustomizer">Function that will be called at publish time to determine the name of the target topic for this <see cref="T"/>.
    /// <para>
    /// For example: <c>WithTopicAddress(msg => $"arn:aws:sns:eu-west-1:00000000:{msg.Tenant}-mymessage")</c> with <c>msg.Tenant</c> of <c>["uk", "au"]</c> would
    /// publish to topics <c>"uk-mymessage"</c> and <c>"au-mymessage"</c> when a message is published with those tenants.
    /// </para>
    /// </param>
    /// <returns>
    /// The current <see cref="TopicAddressPublicationBuilder{T}"/>.
    /// </returns>
    public TopicAddressPublicationBuilder<T> WithTopicAddress(Func<Message, string, string> topicAddressCustomizer)
    {
        TopicAddressCustomizer = topicAddressCustomizer;
        return this;
    }

    /// <inheritdoc />
    public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicAddressPublicationBuilder<T>>();

        logger.LogInformation("Adding SNS publisher for message type '{MessageType}'", typeof(T));

        var arn = Arn.Parse(_topicAddress.TopicArn);

        bus.SerializationRegister.AddSerializer<T>();

        StaticAddressPublicationConfiguration BuildConfiguration(string topicArn)
            => StaticAddressPublicationConfiguration.Build<T>(
                topicArn,
                proxy.GetAwsClientFactory(),
                loggerFactory,
                bus);

        ITopicAddressPublisher publisherConfig = TopicAddressCustomizer != null
            ? DynamicAddressPublicationConfiguration.Build<T>(_topicAddress.TopicArn, TopicAddressCustomizer, BuildConfiguration, loggerFactory)
            : BuildConfiguration(_topicAddress.TopicArn);

        bus.AddMessagePublisher<T>(publisherConfig.Publisher);
        bus.AddMessageBatchPublisher<T>(publisherConfig.BatchPublisher);

        logger.LogInformation(
            "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'",
            arn.Resource,
            typeof(T));
    }
}
