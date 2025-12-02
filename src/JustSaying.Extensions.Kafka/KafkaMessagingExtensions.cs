using JustSaying;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
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
    public static MessagingBusBuilder WithKafkaPublisher<T>(
        this MessagingBusBuilder builder,
        string topic,
        Action<KafkaConfiguration> configure) where T : Message
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var kafkaConfig = new KafkaConfiguration();
        configure(kafkaConfig);
        kafkaConfig.Validate();

        builder.Services(services =>
        {
            services.AddSingleton(kafkaConfig);
        });

        // Configure the publisher in the publications builder
        builder.Publications(pub =>
        {
            pub.WithPublisher<T>((serviceResolver) =>
            {
                var loggerFactory = serviceResolver.ResolveService<ILoggerFactory>();
                var serializationFactory = serviceResolver.ResolveService<IMessageBodySerializationFactory>();
                
                return new KafkaMessagePublisher(
                    topic,
                    kafkaConfig,
                    serializationFactory,
                    loggerFactory);
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds Kafka as a message batch publisher for the specified message type.
    /// </summary>
    public static MessagingBusBuilder WithKafkaBatchPublisher<T>(
        this MessagingBusBuilder builder,
        string topic,
        Action<KafkaConfiguration> configure) where T : Message
    {
        // The KafkaMessagePublisher implements both IMessagePublisher and IMessageBatchPublisher
        return builder.WithKafkaPublisher<T>(topic, configure);
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
