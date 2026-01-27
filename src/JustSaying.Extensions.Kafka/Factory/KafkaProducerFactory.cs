using Confluent.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Factory;

/// <summary>
/// Default factory for creating Kafka producers with standard configuration.
/// </summary>
public class KafkaProducerFactory : IKafkaProducerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public KafkaProducerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IProducer<string, byte[]> CreateProducer(KafkaConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var logger = _loggerFactory.CreateLogger("JustSaying.Kafka.Producer");
        var config = configuration.GetProducerConfig();

        return new ProducerBuilder<string, byte[]>(config)
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
                    logger.LogError("Fatal Kafka producer error: {Code} - {Reason}", error.Code, error.Reason);
                else
                    logger.LogWarning("Kafka producer error: {Code} - {Reason}", error.Code, error.Reason);
            })
            .Build();
    }
}

