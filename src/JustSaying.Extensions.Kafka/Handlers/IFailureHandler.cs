using Confluent.Kafka;
using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Handlers;

/// <summary>
/// Handles message processing failures.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public interface IFailureHandler<T> where T : Message
{
    /// <summary>
    /// Called when message processing fails.
    /// </summary>
    /// <param name="context">The failure context containing message and error details.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task OnFailureAsync(
        MessageFailureContext<T> context,
        Exception exception,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for a failed message.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class MessageFailureContext<T> where T : Message
{
    /// <summary>
    /// The original Kafka consume result.
    /// </summary>
    public ConsumeResult<string, byte[]> KafkaResult { get; set; }

    /// <summary>
    /// The deserialized message (may be null if deserialization failed).
    /// </summary>
    public T Message { get; set; }

    /// <summary>
    /// The source topic.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The partition the message came from.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// The offset of the message.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// Current retry attempt (1-based). Only set for InProcess retry mode.
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// Whether all retries have been exhausted.
    /// </summary>
    public bool RetriesExhausted { get; set; }
}

