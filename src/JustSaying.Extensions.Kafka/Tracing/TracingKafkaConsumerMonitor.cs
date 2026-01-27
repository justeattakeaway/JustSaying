using System.Diagnostics;
using JustSaying.Extensions.Kafka.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Tracing;

/// <summary>
/// Consumer monitor that creates distributed tracing spans using System.Diagnostics.Activity.
/// Can be used alongside OpenTelemetryKafkaConsumerMonitor for combined metrics and traces.
/// </summary>
public class TracingKafkaConsumerMonitor : IKafkaConsumerMonitor
{
    private readonly ILogger _logger;

    // Store current activities per message for correlation
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Activity> _activeActivities
        = new();

    /// <summary>
    /// Creates a new TracingKafkaConsumerMonitor.
    /// </summary>
    public TracingKafkaConsumerMonitor(ILoggerFactory loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<TracingKafkaConsumerMonitor>();
    }

    /// <inheritdoc />
    public void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message
    {
        if (context == null) return;

        // Extract trace context from headers if present
        var parentContext = context.Headers != null
            ? TraceContextPropagator.ExtractTraceContext(context.Headers)
            : null;

        // Start a new consume activity
        var activity = KafkaActivitySource.StartConsumeActivity(
            context.Topic,
            context.Partition,
            context.Offset,
            context.ConsumerGroup ?? "unknown",
            context.Message?.Id.ToString(),
            context.MessageKey,
            parentContext);

        if (activity != null)
        {
            // Add additional tags
            activity.SetTag("messaging.message.timestamp", context.MessageTimestamp);
            activity.SetTag("kafka.consumer.lag_ms", context.LagMilliseconds);

            // Store activity for later correlation
            var key = GetActivityKey(context.Topic, context.Partition, context.Offset);
            _activeActivities[key] = activity;

            _logger?.LogDebug(
                "Started consume activity {TraceId}/{SpanId} for message at {Topic}:{Partition}:{Offset}",
                activity.TraceId, activity.SpanId, context.Topic, context.Partition, context.Offset);
        }
    }

    /// <inheritdoc />
    public void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message
    {
        if (context == null) return;

        var key = GetActivityKey(context.Topic, context.Partition, context.Offset);
        if (_activeActivities.TryRemove(key, out var activity))
        {
            activity.SetTag("messaging.processing_duration_ms", context.ProcessingDuration.TotalMilliseconds);

            if (context.RetryAttempt > 1)
            {
                activity.SetTag("kafka.retry_attempt", context.RetryAttempt);
            }

            KafkaActivitySource.SetSuccess(activity);
            activity.Dispose();

            _logger?.LogDebug(
                "Completed consume activity {TraceId}/{SpanId} for message at {Topic}:{Partition}:{Offset}",
                activity.TraceId, activity.SpanId, context.Topic, context.Partition, context.Offset);
        }
    }

    /// <inheritdoc />
    public void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message
    {
        if (context == null) return;

        var key = GetActivityKey(context.Topic, context.Partition, context.Offset);
        if (_activeActivities.TryGetValue(key, out var activity))
        {
            // Record the exception but don't close yet if retry is pending
            activity.SetTag("kafka.retry_attempt", context.RetryAttempt);
            activity.SetTag("kafka.will_retry", context.WillRetry);

            if (context.Exception != null)
            {
                // Record exception as an activity event
                activity.SetTag("exception.type", context.Exception.GetType().FullName);
                activity.SetTag("exception.message", context.Exception.Message);
            }

            if (!context.WillRetry)
            {
                // No more retries, close the activity
                KafkaActivitySource.RecordException(activity, context.Exception);
                _activeActivities.TryRemove(key, out _);
                activity.Dispose();

                _logger?.LogDebug(
                    "Failed consume activity {TraceId}/{SpanId} for message at {Topic}:{Partition}:{Offset}",
                    activity.TraceId, activity.SpanId, context.Topic, context.Partition, context.Offset);
            }
        }
    }

    /// <inheritdoc />
    public void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message
    {
        if (context == null) return;

        var key = GetActivityKey(context.Topic, context.Partition, context.Offset);
        if (_activeActivities.TryRemove(key, out var activity))
        {
            activity.SetTag("kafka.dead_lettered", true);
            activity.SetTag("kafka.dlt_topic", context.DeadLetterTopic);
            activity.SetTag("kafka.total_attempts", context.TotalAttempts);

            KafkaActivitySource.RecordException(activity, context.Exception);
            activity.Dispose();

            _logger?.LogDebug(
                "Dead-lettered consume activity {TraceId}/{SpanId} for message at {Topic}:{Partition}:{Offset}",
                activity.TraceId, activity.SpanId, context.Topic, context.Partition, context.Offset);
        }
    }

    private static string GetActivityKey(string topic, int partition, long offset)
    {
        return $"{topic}:{partition}:{offset}";
    }
}
