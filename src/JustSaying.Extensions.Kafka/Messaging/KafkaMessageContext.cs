namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Rich context for Kafka messages, providing detailed information about
/// the message source and metadata to handlers.
/// </summary>
public class KafkaMessageContext
{
    /// <summary>
    /// The topic the message came from.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The partition the message came from.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// The offset of the message within the partition.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// The message key (partition key).
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The message timestamp from the Kafka broker.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Message headers as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// When the message was received by the consumer.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Consumer lag in milliseconds (ReceivedAt - Timestamp).
    /// </summary>
    public double LagMilliseconds => (ReceivedAt - Timestamp).TotalMilliseconds;

    /// <summary>
    /// The consumer group ID.
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// The consumer instance ID (optional, may be null for auto-generated IDs).
    /// </summary>
    public string ConsumerId { get; set; }

    /// <summary>
    /// The current retry attempt (1-based). 0 means no retries yet (first attempt).
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// The CloudEvent type, if the message was received as a CloudEvent.
    /// </summary>
    public string CloudEventType { get; set; }

    /// <summary>
    /// The CloudEvent source, if the message was received as a CloudEvent.
    /// </summary>
    public string CloudEventSource { get; set; }

    /// <summary>
    /// The CloudEvent ID, if the message was received as a CloudEvent.
    /// </summary>
    public string CloudEventId { get; set; }
}

