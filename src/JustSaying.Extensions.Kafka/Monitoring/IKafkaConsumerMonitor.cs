using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Monitoring;

/// <summary>
/// Interface for monitoring Kafka consumer events.
/// Implement this interface to collect metrics, logs, or alerts.
/// </summary>
public interface IKafkaConsumerMonitor
{
    /// <summary>
    /// Called when a message is received from Kafka.
    /// </summary>
    void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message;

    /// <summary>
    /// Called when a message is successfully processed.
    /// </summary>
    void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message;

    /// <summary>
    /// Called when a message processing fails.
    /// </summary>
    void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message;

    /// <summary>
    /// Called when a message is sent to DLT.
    /// </summary>
    void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message;
}

/// <summary>
/// Context for message received event.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class MessageReceivedContext<T> where T : Message
{
    /// <summary>
    /// The topic the message was received from.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The partition the message was received from.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// The offset of the message.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// The timestamp on the Kafka message.
    /// </summary>
    public DateTime MessageTimestamp { get; set; }

    /// <summary>
    /// When the message was received by the consumer.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// The deserialized message (may be null if deserialization failed).
    /// </summary>
    public T Message { get; set; }

    /// <summary>
    /// The message key (partition key).
    /// </summary>
    public string MessageKey { get; set; }

    /// <summary>
    /// The consumer group ID.
    /// </summary>
    public string ConsumerGroup { get; set; }

    /// <summary>
    /// Message headers for trace context propagation.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; set; }

    /// <summary>
    /// Consumer lag in milliseconds (ReceivedAt - MessageTimestamp).
    /// </summary>
    public double LagMilliseconds => (ReceivedAt - MessageTimestamp).TotalMilliseconds;
}

/// <summary>
/// Context for message processed event.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class MessageProcessedContext<T> where T : Message
{
    /// <summary>
    /// The topic the message was received from.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The partition the message was received from.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// The offset of the message.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// The deserialized message.
    /// </summary>
    public T Message { get; set; }

    /// <summary>
    /// How long it took to process the message.
    /// </summary>
    public TimeSpan ProcessingDuration { get; set; }

    /// <summary>
    /// Current retry attempt (1-based). Only set for retried messages.
    /// </summary>
    public int RetryAttempt { get; set; }
}

/// <summary>
/// Context for message failed event.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class MessageFailedContext<T> where T : Message
{
    /// <summary>
    /// The topic the message was received from.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The partition the message was received from.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// The offset of the message.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// The deserialized message (may be null if deserialization failed).
    /// </summary>
    public T Message { get; set; }

    /// <summary>
    /// The exception that caused the failure.
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// Current retry attempt (1-based).
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// Whether the message will be retried.
    /// </summary>
    public bool WillRetry { get; set; }
}

/// <summary>
/// Context for message dead-lettered event.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class MessageDeadLetteredContext<T> where T : Message
{
    /// <summary>
    /// The source topic the message was received from.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The dead letter topic the message was sent to.
    /// </summary>
    public string DeadLetterTopic { get; set; }

    /// <summary>
    /// The partition of the source message.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// The offset of the source message.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// The deserialized message (may be null if deserialization failed).
    /// </summary>
    public T Message { get; set; }

    /// <summary>
    /// The exception that caused the dead-lettering.
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// Total number of processing attempts before dead-lettering.
    /// </summary>
    public int TotalAttempts { get; set; }
}

