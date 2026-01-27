using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;

namespace JustSaying.Extensions.Kafka.Factory;

/// <summary>
/// Factory for creating Kafka consumers.
/// Implement this interface to customize consumer creation or for testing.
/// </summary>
public interface IKafkaConsumerFactory
{
    /// <summary>
    /// Creates a consumer with the given configuration.
    /// </summary>
    /// <param name="configuration">The Kafka configuration.</param>
    /// <param name="consumerId">A unique identifier for this consumer instance.</param>
    /// <returns>A configured Kafka consumer.</returns>
    IConsumer<string, byte[]> CreateConsumer(
        KafkaConfiguration configuration,
        string consumerId);
}

