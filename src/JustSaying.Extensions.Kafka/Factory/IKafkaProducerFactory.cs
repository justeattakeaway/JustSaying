using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;

namespace JustSaying.Extensions.Kafka.Factory;

/// <summary>
/// Factory for creating Kafka producers.
/// Implement this interface to customize producer creation or for testing.
/// </summary>
public interface IKafkaProducerFactory
{
    /// <summary>
    /// Creates a producer with the given configuration.
    /// </summary>
    /// <param name="configuration">The Kafka configuration.</param>
    /// <returns>A configured Kafka producer.</returns>
    IProducer<string, byte[]> CreateProducer(KafkaConfiguration configuration);
}

