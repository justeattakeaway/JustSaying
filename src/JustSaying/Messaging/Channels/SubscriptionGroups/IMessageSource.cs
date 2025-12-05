namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <summary>
/// Represents a message source that can be consumed by a subscription group.
/// This abstraction allows supporting multiple messaging transports (SQS, Kafka, etc.)
/// </summary>
public interface IMessageSource
{
    /// <summary>
    /// Gets the name/identifier of this message source (queue name, topic name, etc.)
    /// </summary>
    string Name { get; }
}
