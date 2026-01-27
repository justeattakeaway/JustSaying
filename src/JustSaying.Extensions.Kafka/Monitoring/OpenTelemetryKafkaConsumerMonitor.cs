using System.Diagnostics;
using System.Diagnostics.Metrics;
using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.Monitoring;

/// <summary>
/// Consumer monitor that emits OpenTelemetry metrics.
/// Uses System.Diagnostics.Metrics for compatibility with OpenTelemetry exporters.
/// </summary>
public class OpenTelemetryKafkaConsumerMonitor : IKafkaConsumerMonitor
{
    /// <summary>
    /// The meter name used for all Kafka consumer metrics.
    /// </summary>
    public const string MeterName = "JustSaying.Kafka";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    // Counters
    private static readonly Counter<long> MessagesReceived =
        Meter.CreateCounter<long>(
            "kafka.consumer.messages.received",
            "messages",
            "Total number of messages received from Kafka");

    private static readonly Counter<long> MessagesProcessed =
        Meter.CreateCounter<long>(
            "kafka.consumer.messages.processed",
            "messages",
            "Total number of messages successfully processed");

    private static readonly Counter<long> MessagesFailed =
        Meter.CreateCounter<long>(
            "kafka.consumer.messages.failed",
            "messages",
            "Total number of messages that failed processing");

    private static readonly Counter<long> MessagesDeadLettered =
        Meter.CreateCounter<long>(
            "kafka.consumer.messages.dead_lettered",
            "messages",
            "Total number of messages sent to dead letter topic");

    private static readonly Counter<long> RetryAttempts =
        Meter.CreateCounter<long>(
            "kafka.consumer.retry.attempts",
            "attempts",
            "Total number of retry attempts");

    // Histograms
    private static readonly Histogram<double> ConsumerLag =
        Meter.CreateHistogram<double>(
            "kafka.consumer.lag",
            "ms",
            "Consumer lag in milliseconds (time between message production and consumption)");

    private static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>(
            "kafka.consumer.processing.duration",
            "ms",
            "Time taken to process a message");

    /// <inheritdoc />
    public void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message
    {
        var tags = CreateTags(context.Topic, context.Partition);

        MessagesReceived.Add(1, tags);
        ConsumerLag.Record(context.LagMilliseconds, tags);
    }

    /// <inheritdoc />
    public void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message
    {
        var tags = CreateTags(context.Topic, context.Partition);

        MessagesProcessed.Add(1, tags);
        ProcessingDuration.Record(context.ProcessingDuration.TotalMilliseconds, tags);

        if (context.RetryAttempt > 1)
        {
            RetryAttempts.Add(context.RetryAttempt - 1, tags);
        }
    }

    /// <inheritdoc />
    public void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message
    {
        var tags = new TagList
        {
            { "topic", context.Topic },
            { "partition", context.Partition },
            { "exception_type", context.Exception?.GetType().Name ?? "Unknown" },
            { "will_retry", context.WillRetry }
        };

        MessagesFailed.Add(1, tags);
    }

    /// <inheritdoc />
    public void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message
    {
        var tags = new TagList
        {
            { "topic", context.Topic },
            { "dlt_topic", context.DeadLetterTopic },
            { "partition", context.Partition },
            { "exception_type", context.Exception?.GetType().Name ?? "Unknown" }
        };

        MessagesDeadLettered.Add(1, tags);
    }

    private static TagList CreateTags(string topic, int partition)
    {
        return new TagList
        {
            { "topic", topic },
            { "partition", partition }
        };
    }
}

