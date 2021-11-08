namespace JustSaying.AwsTools.QueueCreation;

/// <summary>
/// An enumeration of subscription types.
/// </summary>
public enum SubscriptionType
{
    /// <summary>
    /// A subscription of type ToTopic ensures that a topic and queue pair exist
    /// such that messages published to a topic are routed to a queue. Usually most useful for events.
    /// </summary>
    ToTopic,
    /// <summary>
    /// A subscription of type PointToPoint ensures that a queue exists that commands
    /// may be directly sent to, without needing to be published to a topic. Usually most useful for commands.
    /// </summary>
    PointToPoint
}