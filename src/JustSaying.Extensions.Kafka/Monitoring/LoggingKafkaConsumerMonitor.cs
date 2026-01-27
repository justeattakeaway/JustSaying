using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Monitoring;

/// <summary>
/// Default monitor implementation that logs consumer events.
/// Registered by default - can be supplemented with additional monitors.
/// </summary>
public class LoggingKafkaConsumerMonitor : IKafkaConsumerMonitor
{
    private readonly ILogger _logger;

    public LoggingKafkaConsumerMonitor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("JustSaying.Kafka.Monitor");
    }

    public void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message
    {
        _logger.LogDebug(
            "Message received: Topic={Topic} Partition={Partition} Offset={Offset} Lag={LagMs:F0}ms",
            context.Topic,
            context.Partition,
            context.Offset,
            context.LagMilliseconds);
    }

    public void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message
    {
        if (context.RetryAttempt > 1)
        {
            _logger.LogInformation(
                "Message processed after retry: Topic={Topic} Partition={Partition} Offset={Offset} Duration={DurationMs:F0}ms Attempt={Attempt}",
                context.Topic,
                context.Partition,
                context.Offset,
                context.ProcessingDuration.TotalMilliseconds,
                context.RetryAttempt);
        }
        else
        {
            _logger.LogDebug(
                "Message processed: Topic={Topic} Partition={Partition} Offset={Offset} Duration={DurationMs:F0}ms",
                context.Topic,
                context.Partition,
                context.Offset,
                context.ProcessingDuration.TotalMilliseconds);
        }
    }

    public void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message
    {
        if (context.WillRetry)
        {
            _logger.LogWarning(
                "Message failed (will retry): Topic={Topic} Partition={Partition} Offset={Offset} Attempt={Attempt} Error={ErrorType}: {ErrorMessage}",
                context.Topic,
                context.Partition,
                context.Offset,
                context.RetryAttempt,
                context.Exception?.GetType().Name ?? "Unknown",
                context.Exception?.Message ?? "No message");
        }
        else
        {
            _logger.LogError(
                "Message failed (no more retries): Topic={Topic} Partition={Partition} Offset={Offset} Attempt={Attempt} Error={ErrorType}: {ErrorMessage}",
                context.Topic,
                context.Partition,
                context.Offset,
                context.RetryAttempt,
                context.Exception?.GetType().Name ?? "Unknown",
                context.Exception?.Message ?? "No message");
        }
    }

    public void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message
    {
        _logger.LogError(
            "Message dead-lettered: Topic={Topic} → DLT={DltTopic} Partition={Partition} Offset={Offset} TotalAttempts={Attempts} Error={ErrorType}: {ErrorMessage}",
            context.Topic,
            context.DeadLetterTopic,
            context.Partition,
            context.Offset,
            context.TotalAttempts,
            context.Exception?.GetType().Name ?? "Unknown",
            context.Exception?.Message ?? "No message");
    }
}

