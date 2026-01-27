using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Extensions.Kafka.Partitioning;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Fluent;

/// <summary>
/// Fluent configuration builder for Kafka publishers.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class KafkaPublisherBuilder<T> where T : Message
{
    private readonly string _topic;
    private readonly KafkaConfiguration _configuration = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaPublisherBuilder{T}"/> class.
    /// </summary>
    /// <param name="topic">The Kafka topic name.</param>
    public KafkaPublisherBuilder(string topic)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
    }

    /// <summary>
    /// Configures the Kafka bootstrap servers.
    /// </summary>
    public KafkaPublisherBuilder<T> WithBootstrapServers(string bootstrapServers)
    {
        _configuration.BootstrapServers = bootstrapServers;
        return this;
    }

    /// <summary>
    /// Enables or disables CloudEvents format.
    /// </summary>
    public KafkaPublisherBuilder<T> WithCloudEvents(bool enable = true, string source = "urn:justsaying")
    {
        _configuration.EnableCloudEvents = enable;
        _configuration.CloudEventsSource = source;
        return this;
    }

    /// <summary>
    /// Configures the Kafka producer.
    /// </summary>
    public KafkaPublisherBuilder<T> WithProducerConfig(Action<ProducerConfig> configure)
    {
        var config = _configuration.ProducerConfig ?? new ProducerConfig();
        configure(config);
        _configuration.ProducerConfig = config;
        return this;
    }

    /// <summary>
    /// Configures additional Kafka producer settings.
    /// </summary>
    public KafkaPublisherBuilder<T> WithProducerSetting(string key, string value)
    {
        var config = _configuration.ProducerConfig ?? new ProducerConfig();
        config.Set(key, value);
        _configuration.ProducerConfig = config;
        return this;
    }

    #region Partitioning Configuration

    /// <summary>
    /// Configures a custom partition key strategy.
    /// </summary>
    /// <param name="strategy">The partition key strategy to use.</param>
    public KafkaPublisherBuilder<T> WithPartitionKeyStrategy(IPartitionKeyStrategy strategy)
    {
        _configuration.PartitionKeyStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        return this;
    }

    /// <summary>
    /// Uses message ID as the partition key.
    /// </summary>
    public KafkaPublisherBuilder<T> WithMessageIdPartitioning()
    {
        _configuration.PartitionKeyStrategy = MessageIdPartitionKeyStrategy.Instance;
        return this;
    }

    /// <summary>
    /// Uses message.UniqueKey() as the partition key (default behavior).
    /// </summary>
    public KafkaPublisherBuilder<T> WithUniqueKeyPartitioning()
    {
        _configuration.PartitionKeyStrategy = UniqueKeyPartitionKeyStrategy.Instance;
        return this;
    }

    /// <summary>
    /// Uses round-robin partitioning (null key, Kafka distributes evenly).
    /// </summary>
    public KafkaPublisherBuilder<T> WithRoundRobinPartitioning()
    {
        _configuration.PartitionKeyStrategy = RoundRobinPartitionKeyStrategy.Instance;
        return this;
    }

    /// <summary>
    /// Uses sticky partitioning - messages go to the same partition for a time period.
    /// </summary>
    /// <param name="stickyDuration">How long to stick to one partition. Default is 1 second.</param>
    public KafkaPublisherBuilder<T> WithStickyPartitioning(TimeSpan? stickyDuration = null)
    {
        _configuration.PartitionKeyStrategy = new StickyPartitionKeyStrategy(stickyDuration);
        return this;
    }

    /// <summary>
    /// Uses time-based partitioning - messages are routed based on their timestamp.
    /// </summary>
    /// <param name="windowSize">The time window size. Messages within the same window go to the same partition.</param>
    public KafkaPublisherBuilder<T> WithTimeBasedPartitioning(TimeSpan windowSize)
    {
        _configuration.PartitionKeyStrategy = new TimeBasedPartitionKeyStrategy(windowSize);
        return this;
    }

    /// <summary>
    /// Uses consistent hash partitioning based on a message property.
    /// </summary>
    /// <param name="propertySelector">Function to select the property to use as partition key.</param>
    public KafkaPublisherBuilder<T> WithConsistentHashPartitioning(Func<T, string> propertySelector)
    {
        _configuration.PartitionKeyStrategy = new ConsistentHashPartitionKeyStrategy<T>(propertySelector);
        return this;
    }

    /// <summary>
    /// Uses a custom delegate for partition key generation.
    /// </summary>
    /// <param name="keySelector">Function that takes (message, topic) and returns the partition key.</param>
    public KafkaPublisherBuilder<T> WithCustomPartitioning(Func<T, string, string> keySelector)
    {
        _configuration.PartitionKeyStrategy = new DelegatePartitionKeyStrategy<T>(keySelector);
        return this;
    }

    #endregion

    /// <summary>
    /// Builds the Kafka message publisher.
    /// </summary>
    internal KafkaMessagePublisher Build(
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory)
    {
        _configuration.Validate();
        return new KafkaMessagePublisher(_topic, _configuration, serializationFactory, loggerFactory);
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public KafkaConfiguration GetConfiguration() => _configuration;
}

/// <summary>
/// Fluent configuration builder for Kafka consumers.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public class KafkaConsumerBuilder<T> where T : Message
{
    private readonly string _topic;
    private readonly KafkaConfiguration _configuration = new();

    public KafkaConsumerBuilder(string topic)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
    }

    /// <summary>
    /// Configures the Kafka bootstrap servers.
    /// </summary>
    public KafkaConsumerBuilder<T> WithBootstrapServers(string bootstrapServers)
    {
        _configuration.BootstrapServers = bootstrapServers;
        return this;
    }

    /// <summary>
    /// Configures the consumer group ID.
    /// </summary>
    public KafkaConsumerBuilder<T> WithGroupId(string groupId)
    {
        _configuration.GroupId = groupId;
        return this;
    }

    /// <summary>
    /// Enables or disables CloudEvents format.
    /// </summary>
    public KafkaConsumerBuilder<T> WithCloudEvents(bool enable = true, string source = "urn:justsaying")
    {
        _configuration.EnableCloudEvents = enable;
        _configuration.CloudEventsSource = source;
        return this;
    }

    /// <summary>
    /// Configures the Kafka consumer.
    /// </summary>
    public KafkaConsumerBuilder<T> WithConsumerConfig(Action<ConsumerConfig> configure)
    {
        var config = _configuration.ConsumerConfig ?? new ConsumerConfig();
        configure(config);
        _configuration.ConsumerConfig = config;
        return this;
    }

    /// <summary>
    /// Configures additional Kafka consumer settings.
    /// </summary>
    public KafkaConsumerBuilder<T> WithConsumerSetting(string key, string value)
    {
        var config = _configuration.ConsumerConfig ?? new ConsumerConfig();
        config.Set(key, value);
        _configuration.ConsumerConfig = config;
        return this;
    }

    /// <summary>
    /// Builds the Kafka message consumer.
    /// </summary>
    internal KafkaMessageConsumer Build(
        IMessageBodySerializationFactory serializationFactory,
        ILoggerFactory loggerFactory)
    {
        _configuration.Validate();
        return new KafkaMessageConsumer(_topic, _configuration, serializationFactory, loggerFactory);
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public KafkaConfiguration GetConfiguration() => _configuration;
}
