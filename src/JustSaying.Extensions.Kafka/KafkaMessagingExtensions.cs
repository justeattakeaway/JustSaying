using JustSaying;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Fluent;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring Kafka transport in JustSaying.
/// </summary>
public static class KafkaMessagingExtensions
{
    /// <summary>
    /// Adds Kafka as a message publisher for the specified message type.
    /// </summary>
    public static IServiceCollection WithKafkaPublisher<T>(
        this IServiceCollection services,
        string topic,
        Action<KafkaConfiguration> configure) where T : Message
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var kafkaConfig = new KafkaConfiguration();
        configure(kafkaConfig);
        kafkaConfig.Validate();

        // Register the publisher
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var serializationFactory = sp.GetRequiredService<IMessageBodySerializationFactory>();
            
            return new KafkaMessagePublisher(
                topic,
                kafkaConfig,
                serializationFactory,
                loggerFactory);
        });

        return services;
    }

    /// <summary>
    /// Adds Kafka as a message batch publisher for the specified message type.
    /// </summary>
    public static IServiceCollection WithKafkaBatchPublisher<T>(
        this IServiceCollection services,
        string topic,
        Action<KafkaConfiguration> configure) where T : Message
    {
        // The KafkaMessagePublisher implements both IMessagePublisher and IMessageBatchPublisher
        return services.WithKafkaPublisher<T>(topic, configure);
    }

    /// <summary>
    /// Adds a Kafka topic subscription for the specified message type.
    /// </summary>
    public static IServiceCollection AddKafkaSubscription<T>(
        this IServiceCollection services,
        string topic,
        Action<KafkaSubscriptionBuilder<T>> configure) where T : Message
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var kafkaBuilder = new KafkaSubscriptionBuilder<T>(topic);
        configure(kafkaBuilder);

        // Register the subscription in the DI container
        services.AddSingleton(kafkaBuilder);

        return services;
    }

    /// <summary>
    /// Creates a Kafka message consumer for the specified topic.
    /// </summary>
    public static KafkaMessageConsumer CreateKafkaConsumer(
        this IServiceProvider serviceProvider,
        string topic,
        Action<KafkaConfiguration> configure)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var kafkaConfig = new KafkaConfiguration();
        configure(kafkaConfig);
        kafkaConfig.Validate();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var serializationFactory = serviceProvider.GetRequiredService<IMessageBodySerializationFactory>();

        return new KafkaMessageConsumer(
            topic,
            kafkaConfig,
            serializationFactory,
            loggerFactory);
    }
}
