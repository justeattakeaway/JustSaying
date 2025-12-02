using JustSaying.AwsTools;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Fluent;

/// <summary>
/// Builder for configuring Kafka topic publications.
/// </summary>
/// <typeparam name="T">The message type to publish.</typeparam>
public class KafkaPublicationBuilder<T> : IPublicationBuilder<T> where T : Message
{
    private readonly string _topic;
    private readonly KafkaConfiguration _configuration = new();
    private readonly KafkaMessagingConfiguration _globalConfig;

    public KafkaPublicationBuilder(string topic, KafkaMessagingConfiguration globalConfig = null)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _globalConfig = globalConfig;

        // Apply global defaults if available
        if (_globalConfig != null)
        {
            _configuration.BootstrapServers = _globalConfig.BootstrapServers;
            _configuration.EnableCloudEvents = _globalConfig.EnableCloudEvents;
            _configuration.CloudEventsSource = _globalConfig.CloudEventsSource;
        }
    }

    /// <summary>
    /// Configures the Kafka bootstrap servers.
    /// </summary>
    public KafkaPublicationBuilder<T> WithBootstrapServers(string bootstrapServers)
    {
        _configuration.BootstrapServers = bootstrapServers;
        return this;
    }

    /// <summary>
    /// Enables or disables CloudEvents format.
    /// </summary>
    public KafkaPublicationBuilder<T> WithCloudEvents(bool enable = true, string source = "urn:justsaying")
    {
        _configuration.EnableCloudEvents = enable;
        _configuration.CloudEventsSource = source;
        return this;
    }

    /// <summary>
    /// Configures the Kafka producer settings.
    /// </summary>
    public KafkaPublicationBuilder<T> WithProducerConfig(Action<Confluent.Kafka.ProducerConfig> configure)
    {
        var config = _configuration.ProducerConfig ?? new Confluent.Kafka.ProducerConfig();
        configure(config);
        _configuration.ProducerConfig = config;
        return this;
    }

    /// <summary>
    /// Configures the publication for the JustSayingBus.
    /// </summary>
    public void Configure(JustSayingBus bus, IAwsClientFactoryProxy proxy, ILoggerFactory loggerFactory)
    {
        _configuration.Validate();
        
        var logger = loggerFactory.CreateLogger<KafkaPublicationBuilder<T>>();
        logger.LogInformation("Adding Kafka publisher for message type '{MessageType}' to topic '{Topic}'",
            typeof(T), _topic);
        
        // Create the Kafka publisher
        var publisher = new KafkaMessagePublisher(
            _topic,
            _configuration,
            bus.MessageBodySerializerFactory,
            loggerFactory);
        
        // Register with the bus
        bus.AddMessagePublisher<T>(publisher);
    }
}
