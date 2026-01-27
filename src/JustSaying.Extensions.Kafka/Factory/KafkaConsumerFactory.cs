using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Factory;

/// <summary>
/// Default factory for creating Kafka consumers with standard configuration.
/// </summary>
public class KafkaConsumerFactory : IKafkaConsumerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public KafkaConsumerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IConsumer<string, byte[]> CreateConsumer(
        KafkaConfiguration configuration,
        string consumerId)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var logger = _loggerFactory.CreateLogger($"JustSaying.Kafka.Consumer.{consumerId ?? "default"}");
        var config = configuration.GetConsumerConfig();

        return new ConsumerBuilder<string, byte[]>(config)
            .SetLogHandler((_, msg) =>
            {
                if (msg.Level <= SyslogLevel.Warning)
                    logger.LogWarning("[Kafka] {Message}", msg.Message);
                else
                    logger.LogDebug("[Kafka] {Message}", msg.Message);
            })
            .SetErrorHandler((_, error) =>
            {
                if (error.IsFatal)
                    logger.LogError("Fatal Kafka error: {Code} - {Reason}", error.Code, error.Reason);
                else
                    logger.LogWarning("Kafka error: {Code} - {Reason}", error.Code, error.Reason);
            })
            .SetStatisticsHandler((_, json) =>
            {
                logger.LogTrace("Kafka statistics: {Statistics}", json);
            })
            .Build();
    }
}

