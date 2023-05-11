using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Receive;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <summary>
/// The settings used by <see cref="SubscriptionGroupFactory"/> to be create
/// a <see cref="ISubscriptionGroup"/>.
/// </summary>
public sealed class SubscriptionGroupSettings : ISubscriptionGroupSettings
{
    internal SubscriptionGroupSettings(
        string name,
        int concurrencyLimit,
        int bufferSize,
        TimeSpan receiveBufferReadTimeout,
        TimeSpan receiveMessagesWaitTime,
        int multiplexerCapacity,
        int prefetch,
        IReadOnlyCollection<ISqsQueue> queues,
        TimeSpan notReceivingBusyWaitInterval)
    {
        ConcurrencyLimit = concurrencyLimit;
        BufferSize = bufferSize;
        ReceiveBufferReadTimeout = receiveBufferReadTimeout;
        ReceiveMessagesWaitTime = receiveMessagesWaitTime;
        MultiplexerCapacity = multiplexerCapacity;
        Prefetch = prefetch;
        Queues = queues;
        Name = name;
        NotReceivingBusyWaitInterval = notReceivingBusyWaitInterval;
    }

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

    /// <summary>
    /// The name of the created <see cref="ISubscriptionGroup"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A collection of <see cref="ISqsQueue"/> to read messages from.
    /// </summary>
    public IReadOnlyCollection<ISqsQueue> Queues { get; }

    /// <summary>
    /// Delay interval to use during busy wait when <see cref="IMessageReceivePauseSignal"/> is set to pause receiving.
    /// A larger value may reduce CPU usage while paused, but may delay when messages start being received.
    /// </summary>
    public TimeSpan NotReceivingBusyWaitInterval { get; }
}
