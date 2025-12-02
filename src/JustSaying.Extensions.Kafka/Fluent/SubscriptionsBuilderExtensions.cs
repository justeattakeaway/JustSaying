using JustSaying.Fluent;
using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Fluent;

/// <summary>
/// Extension methods for <see cref="SubscriptionsBuilder"/> to support Kafka subscriptions.
/// </summary>
public static class SubscriptionsBuilderExtensions
{
    /// <summary>
    /// Configures a Kafka topic subscription for the default topic name.
    /// </summary>
    /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
    /// <param name="builder">The subscriptions builder.</param>
    /// <returns>The current <see cref="SubscriptionsBuilder"/>.</returns>
    public static SubscriptionsBuilder ForKafka<T>(this SubscriptionsBuilder builder)
        where T : Message
    {
        return builder.ForKafka<T>(typeof(T).Name.ToLowerInvariant(), null);
    }

    /// <summary>
    /// Configures a Kafka topic subscription.
    /// </summary>
    /// <typeparam name="T">The type of the message to subscribe to.</typeparam>
    /// <param name="builder">The subscriptions builder.</param>
    /// <param name="topic">The Kafka topic to subscribe to.</param>
    /// <param name="configure">A delegate to configure the Kafka subscription.</param>
    /// <returns>The current <see cref="SubscriptionsBuilder"/>.</returns>
    public static SubscriptionsBuilder ForKafka<T>(
        this SubscriptionsBuilder builder,
        string topic,
        Action<KafkaSubscriptionBuilder<T>> configure = null)
        where T : Message
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic name cannot be null or empty.", nameof(topic));

        // Get global Kafka config if available
        var globalConfig = builder.BusBuilder.MessagingConfig.GetKafkaConfig();

        var kafkaBuilder = new KafkaSubscriptionBuilder<T>(topic, globalConfig);
        
        configure?.Invoke(kafkaBuilder);

        // Add the Kafka builder as a custom subscription using the new public method
        builder.WithCustomSubscription(kafkaBuilder);

        return builder;
    }
}
