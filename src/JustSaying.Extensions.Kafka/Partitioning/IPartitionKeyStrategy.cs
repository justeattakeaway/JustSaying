using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Partitioning;

/// <summary>
/// Strategy for generating partition keys for Kafka messages.
/// The partition key determines which partition a message is sent to.
/// </summary>
public interface IPartitionKeyStrategy
{
    /// <summary>
    /// Gets the partition key for a message.
    /// </summary>
    /// <param name="message">The message to get the key for.</param>
    /// <param name="topic">The target topic.</param>
    /// <returns>The partition key. If null, Kafka will use round-robin partitioning.</returns>
    string GetPartitionKey(Message message, string topic);
}

/// <summary>
/// Strategy for generating partition keys with access to message type.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public interface IPartitionKeyStrategy<T> where T : Message
{
    /// <summary>
    /// Gets the partition key for a typed message.
    /// </summary>
    /// <param name="message">The message to get the key for.</param>
    /// <param name="topic">The target topic.</param>
    /// <returns>The partition key. If null, Kafka will use round-robin partitioning.</returns>
    string GetPartitionKey(T message, string topic);
}
