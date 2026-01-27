using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Handlers;
using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Models;

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

    #region Failure & Retry Configuration

    /// <summary>
    /// Gets or sets the dead letter topic name.
    /// When set, failed messages will be forwarded to this topic after retries are exhausted.
    /// If null, failed messages are logged and committed (no DLT).
    /// </summary>
    public string DeadLetterTopic { get; set; }

    /// <summary>
    /// Gets or sets the failure topic name (for topic chaining mode).
    /// When using InProcess retry mode, this is ignored - use DeadLetterTopic instead.
    /// When using TopicChaining mode, failed messages are forwarded to this topic.
    /// </summary>
    public string FailureTopic { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds before processing messages.
    /// Used for retry topics in TopicChaining mode.
    /// Default is 0 (no delay).
    /// </summary>
    public uint DelayInMs { get; set; } = 0;

    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryConfiguration Retry { get; set; } = new();

    /// <summary>
    /// Optional factory for creating a custom failure handler.
    /// If provided, standard retry/DLT behavior is bypassed.
    /// </summary>
    public Func<IServiceProvider, IFailureHandler<Message>> FailureHandlerFactory { get; set; }

    #endregion

    #region Scaling Configuration

    /// <summary>
    /// Gets or sets the number of consumer instances to create for this subscription.
    /// Each consumer will be registered as a separate IHostedService.
    /// Default is 1.
    /// </summary>
    /// <remarks>
    /// Multiple consumers within the same consumer group will share partitions.
    /// Set this to a value less than or equal to the number of partitions for optimal scaling.
    /// </remarks>
    public uint NumberOfConsumers { get; set; } = 1;

    #endregion

    #region Partitioning Configuration

    /// <summary>
    /// Gets or sets the partition key strategy for message publishing.
    /// Default is null (uses message.UniqueKey() for publishers, message.Id for typed producers).
    /// </summary>
    /// <remarks>
    /// Built-in strategies:
    /// - MessageIdPartitionKeyStrategy: Uses message ID
    /// - UniqueKeyPartitionKeyStrategy: Uses message.UniqueKey()
    /// - RoundRobinPartitionKeyStrategy: Round-robin across partitions
    /// - StickyPartitionKeyStrategy: Sticky to one partition for a time period
    /// - ConsistentHashPartitionKeyStrategy: Hash-based on a property
    /// - TimeBasedPartitionKeyStrategy: Based on message timestamp
    /// </remarks>
    public IPartitionKeyStrategy PartitionKeyStrategy { get; set; }

    #endregion

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
