using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
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
    internal KafkaConfiguration GetConfiguration() => _configuration;
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
    internal KafkaConfiguration GetConfiguration() => _configuration;
}
