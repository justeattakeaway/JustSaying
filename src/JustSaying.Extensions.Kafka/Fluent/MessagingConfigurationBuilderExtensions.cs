using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Fluent;

namespace JustSaying.Extensions.Kafka.Fluent;

/// <summary>
/// Extension methods for configuring global Kafka settings.
/// </summary>
public static class MessagingConfigurationBuilderExtensions
{
    private const string KafkaConfigKey = "JustSaying.Kafka.GlobalConfig";

    /// <summary>
    /// Configures global Kafka settings that will be inherited by all Kafka publications and subscriptions.
    /// </summary>
    /// <param name="builder">The messaging configuration builder.</param>
    /// <param name="configure">Action to configure Kafka settings.</param>
    /// <returns>The messaging configuration builder for method chaining.</returns>
    public static MessagingConfigurationBuilder WithKafka(
        this MessagingConfigurationBuilder builder,
        Action<KafkaMessagingConfiguration> configure)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var config = GetOrCreateKafkaConfig(builder);
        configure(config);

        return builder;
    }

    /// <summary>
    /// Gets the global Kafka configuration if it exists.
    /// </summary>
    internal static KafkaMessagingConfiguration GetKafkaConfig(this MessagingConfigurationBuilder builder)
    {
        return builder.BusBuilder.Properties.TryGetValue(KafkaConfigKey, out var config)
            ? config as KafkaMessagingConfiguration
            : null;
    }

    /// <summary>
    /// Gets or creates the global Kafka configuration.
    /// </summary>
    private static KafkaMessagingConfiguration GetOrCreateKafkaConfig(MessagingConfigurationBuilder builder)
    {
        if (!builder.BusBuilder.Properties.TryGetValue(KafkaConfigKey, out var config))
        {
            config = new KafkaMessagingConfiguration();
            builder.BusBuilder.Properties[KafkaConfigKey] = config;
        }

        return config as KafkaMessagingConfiguration;
    }
}
