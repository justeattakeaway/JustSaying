namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <summary>
/// A common interface for default and override subscription group settings.
/// </summary>
public interface ISubscriptionGroupSettings
{
    /// <summary>
    /// Gets the maximum number of messages that may be processed at once.
    /// </summary>
    public int ConcurrencyLimit { get; }

    /// <summary>
    /// Gets the size of the in memory buffer for each queue.
    /// </summary>
    public int BufferSize { get; }

    /// <summary>
    /// Gets the maximum amount of time to wait for messages to be available on each SQS queue in the
    /// created <see cref="ISubscriptionGroup"/> before resetting the connection.
    /// </summary>
    public TimeSpan ReceiveBufferReadTimeout { get; }

    /// <summary>
    /// Gets the duration SQS will wait for a message before returning if there are no messages.
    /// </summary>
    public TimeSpan ReceiveMessagesWaitTime { get; }

    /// <summary>
    /// Gets the size of the shared buffer for all queues in the created <see cref="ISubscriptionGroup"/>.
    /// </summary>
    public int MultiplexerCapacity { get; }

    /// <summary>
    /// Gets the maxiumum number of messages to fetch from SQS in each request.
    /// </summary>
    public int Prefetch { get; }
}
