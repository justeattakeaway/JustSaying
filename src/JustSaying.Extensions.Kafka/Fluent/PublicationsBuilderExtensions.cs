using JustSaying.Fluent;
using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Fluent;

/// <summary>
/// Extension methods for <see cref="PublicationsBuilder"/> to support Kafka publications.
/// </summary>
public static class PublicationsBuilderExtensions
{
    /// <summary>
    /// Configures a Kafka topic publication for the default topic name.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="builder">The publications builder.</param>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    public static PublicationsBuilder WithKafka<T>(this PublicationsBuilder builder)
        where T : Message
    {
        return builder.WithKafka<T>(typeof(T).Name.ToLowerInvariant(), null);
    }

    /// <summary>
    /// Configures a Kafka topic publication.
    /// </summary>
    /// <typeparam name="T">The type of the message to publish.</typeparam>
    /// <param name="builder">The publications builder.</param>
    /// <param name="topic">The Kafka topic to publish to.</param>
    /// <param name="configure">A delegate to configure the Kafka publication.</param>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    public static PublicationsBuilder WithKafka<T>(
        this PublicationsBuilder builder,
        string topic,
        Action<KafkaPublicationBuilder<T>> configure = null)
        where T : Message
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic name cannot be null or empty.", nameof(topic));

        // Get global Kafka config if available
        var globalConfig = builder.BusBuilder.MessagingConfig.GetKafkaConfig();

        var kafkaBuilder = new KafkaPublicationBuilder<T>(topic, globalConfig);
        
        configure?.Invoke(kafkaBuilder);

        // Add the Kafka builder as a custom publication using the new public method
        builder.WithCustomPublication(kafkaBuilder);

        return builder;
    }
}
