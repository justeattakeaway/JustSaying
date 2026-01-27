using Confluent.Kafka;
using JustSaying.Messaging;
using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// Typed Kafka producer interface.
/// The type parameter is a marker type for DI registration, allowing multiple
/// producer configurations to be registered with different settings.
/// </summary>
/// <typeparam name="TProducerType">A marker type to identify this producer configuration.</typeparam>
public interface IKafkaProducer<TProducerType> : IDisposable where TProducerType : class
{
    /// <summary>
    /// Produces a message asynchronously and waits for delivery confirmation.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="topic">The topic to produce to.</param>
    /// <param name="message">The message to produce.</param>
    /// <param name="key">Optional partition key. If null, uses message.Id.</param>
    /// <param name="metadata">Optional publish metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was delivered successfully.</returns>
    Task<bool> ProduceAsync<TMessage>(
        string topic,
        TMessage message,
        string key = null,
        PublishMetadata metadata = null,
        CancellationToken cancellationToken = default) where TMessage : Message;

    /// <summary>
    /// Produces a message with a delivery callback (non-blocking fire-and-forget).
    /// The callback is invoked when the delivery report is received from the broker.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="topic">The topic to produce to.</param>
    /// <param name="message">The message to produce.</param>
    /// <param name="deliveryHandler">Callback invoked with the delivery report.</param>
    /// <param name="key">Optional partition key. If null, uses message.Id.</param>
    /// <param name="metadata">Optional publish metadata.</param>
    void Produce<TMessage>(
        string topic,
        TMessage message,
        Action<DeliveryReport<string, byte[]>> deliveryHandler,
        string key = null,
        PublishMetadata metadata = null) where TMessage : Message;

    /// <summary>
    /// Flushes all pending messages to the broker.
    /// Call this before disposing to ensure all messages are delivered.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for flush.</param>
    /// <returns>Number of messages still in queue (0 means all delivered).</returns>
    int Flush(TimeSpan timeout);
}

