namespace JustSaying.Messaging.Metadata;

/// <summary>
/// Describes a subscription registered with the messaging bus, captured when the
/// fluent builders are configured and before any AWS infrastructure is provisioned.
/// </summary>
public sealed class SubscriptionMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionMetadata"/> class.
    /// </summary>
    /// <param name="queueName">The resolved name of the queue messages are received from.</param>
    /// <param name="topicName">The resolved name of the SNS topic the queue is subscribed to, or <see langword="null"/> for point-to-point subscriptions.</param>
    /// <param name="subscriptionGroupName">The name of the subscription group the queue belongs to.</param>
    /// <param name="rawMessageDelivery">Whether the subscription uses raw message delivery.</param>
    /// <param name="messages">The message type(s) handled from the queue.</param>
    /// <param name="region">The AWS region of the queue when it was addressed explicitly (for example by ARN or URL), or <see langword="null"/> when the queue is in the bus's configured region.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="queueName"/> or <paramref name="messages"/> is <see langword="null"/>.
    /// </exception>
    public SubscriptionMetadata(
        string queueName,
        string topicName,
        string subscriptionGroupName,
        bool rawMessageDelivery,
        IReadOnlyList<MessageTypeMetadata> messages,
        string region = null)
    {
        QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        TopicName = topicName;
        SubscriptionGroupName = subscriptionGroupName;
        RawMessageDelivery = rawMessageDelivery;
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        Region = string.IsNullOrEmpty(region) ? null : region;
    }

    /// <summary>
    /// Gets the resolved name of the queue messages are received from.
    /// </summary>
    public string QueueName { get; }

    /// <summary>
    /// Gets the resolved name of the SNS topic the queue is subscribed to, or
    /// <see langword="null"/> for point-to-point subscriptions.
    /// </summary>
    public string TopicName { get; }

    /// <summary>
    /// Gets the name of the subscription group the queue belongs to.
    /// </summary>
    public string SubscriptionGroupName { get; }

    /// <summary>
    /// Gets a value indicating whether the subscription uses raw message delivery.
    /// </summary>
    public bool RawMessageDelivery { get; }

    /// <summary>
    /// Gets the message type(s) handled from the queue.
    /// </summary>
    public IReadOnlyList<MessageTypeMetadata> Messages { get; }

    /// <summary>
    /// Gets the AWS region of the queue when it was addressed explicitly (for example by
    /// ARN or URL), or <see langword="null"/> when the queue is in the bus's configured region.
    /// </summary>
    public string Region { get; }
}
