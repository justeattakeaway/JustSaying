namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <summary>
/// Specifies how the concurrency limit is applied to message processing
/// in a <see cref="ISubscriptionGroup"/>.
/// </summary>
public enum ConcurrencyLimitType
{
    /// <summary>
    /// The limit controls the maximum number of messages that may be processed simultaneously.
    /// This is the default behavior.
    /// </summary>
    InFlightMessages = 0,

    /// <summary>
    /// The limit controls the maximum number of messages that may be processed per second.
    /// </summary>
    MessagesPerSecond = 1,
}
