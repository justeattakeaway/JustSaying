using Confluent.Kafka;

namespace JustSaying.Extensions.Kafka.Configuration;

/// <summary>
/// Configuration for Kafka transport.
/// </summary>
public class KafkaConfiguration
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; }

    /// <summary>
    /// Gets or sets the consumer group ID.
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Gets or sets the producer configuration.
    /// </summary>
    public ProducerConfig ProducerConfig { get; set; }

    /// <summary>
    /// Gets or sets the consumer configuration.
    /// </summary>
    public ConsumerConfig ConsumerConfig { get; set; }

    /// <summary>
    /// Gets or sets whether to enable CloudEvents format.
    /// Default is true.
    /// </summary>
    public bool EnableCloudEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the CloudEvents source identifier.
    /// </summary>
    public string CloudEventsSource { get; set; } = "urn:justsaying";

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(BootstrapServers))
        {
            throw new InvalidOperationException("Bootstrap servers must be configured.");
        }
    }

    /// <summary>
    /// Gets the effective producer configuration.
    /// </summary>
    public ProducerConfig GetProducerConfig()
    {
        var config = ProducerConfig ?? new ProducerConfig();
        if (string.IsNullOrEmpty(config.BootstrapServers))
        {
            config.BootstrapServers = BootstrapServers;
        }
        return config;
    }

    /// <summary>
    /// Gets the effective consumer configuration.
    /// </summary>
    public ConsumerConfig GetConsumerConfig()
    {
        var config = ConsumerConfig ?? new ConsumerConfig();
        if (string.IsNullOrEmpty(config.BootstrapServers))
        {
            config.BootstrapServers = BootstrapServers;
        }
        if (string.IsNullOrEmpty(config.GroupId) && !string.IsNullOrEmpty(GroupId))
        {
            config.GroupId = GroupId;
        }
        config.AutoOffsetReset ??= AutoOffsetReset.Earliest;
        config.EnableAutoCommit ??= false;
        return config;
    }
}
