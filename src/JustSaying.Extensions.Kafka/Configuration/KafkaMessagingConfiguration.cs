namespace JustSaying.Extensions.Kafka.Configuration;

/// <summary>
/// Global Kafka configuration that can be set at the messaging level
/// and inherited by all Kafka publications and subscriptions.
/// </summary>
public class KafkaMessagingConfiguration
{
    /// <summary>
    /// Gets or sets the default Kafka bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; }

    /// <summary>
    /// Gets or sets whether CloudEvents format is enabled by default.
    /// </summary>
    public bool EnableCloudEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the default CloudEvents source URI.
    /// </summary>
    public string CloudEventsSource { get; set; } = "urn:justsaying";

    /// <summary>
    /// Gets or sets the default consumer group ID prefix.
    /// Individual subscriptions can override this.
    /// </summary>
    public string DefaultGroupIdPrefix { get; set; }

    /// <summary>
    /// Creates a copy of this configuration with values from another configuration
    /// applied on top (child configuration takes precedence).
    /// </summary>
    public KafkaConfiguration MergeWith(KafkaConfiguration childConfig)
    {
        var merged = new KafkaConfiguration
        {
            BootstrapServers = childConfig.BootstrapServers ?? BootstrapServers,
            EnableCloudEvents = childConfig.EnableCloudEvents,
            CloudEventsSource = childConfig.CloudEventsSource ?? CloudEventsSource,
            GroupId = childConfig.GroupId,
            ProducerConfig = childConfig.ProducerConfig,
            ConsumerConfig = childConfig.ConsumerConfig
        };

        return merged;
    }

    /// <summary>
    /// Validates that required configuration is set.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BootstrapServers))
        {
            throw new InvalidOperationException(
                "Kafka BootstrapServers must be configured either globally via WithKafka() or per topic.");
        }
    }
}
