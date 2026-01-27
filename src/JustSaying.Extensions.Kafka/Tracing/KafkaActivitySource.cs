using System.Diagnostics;

namespace JustSaying.Extensions.Kafka.Tracing;

/// <summary>
/// Central ActivitySource for Kafka distributed tracing.
/// Uses W3C Trace Context for propagation.
/// </summary>
public static class KafkaActivitySource
{
    /// <summary>
    /// The name of the ActivitySource used for Kafka tracing.
    /// </summary>
    public const string SourceName = "JustSaying.Kafka";

    /// <summary>
    /// The version of the ActivitySource.
    /// </summary>
    public const string SourceVersion = "1.0.0";

    /// <summary>
    /// The ActivitySource instance.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, SourceVersion);

    // Activity names
    public const string ProduceActivityName = "kafka.produce";
    public const string ConsumeActivityName = "kafka.consume";
    public const string ProcessActivityName = "kafka.process";

    // Semantic convention tag names (following OpenTelemetry conventions)
    public const string MessagingSystemTag = "messaging.system";
    public const string MessagingDestinationTag = "messaging.destination.name";
    public const string MessagingDestinationKindTag = "messaging.destination.kind";
    public const string MessagingOperationTag = "messaging.operation";
    public const string MessagingMessageIdTag = "messaging.message.id";
    public const string MessagingKafkaPartitionTag = "messaging.kafka.partition";
    public const string MessagingKafkaOffsetTag = "messaging.kafka.offset";
    public const string MessagingKafkaConsumerGroupTag = "messaging.kafka.consumer.group";
    public const string MessagingKafkaMessageKeyTag = "messaging.kafka.message.key";

    /// <summary>
    /// Creates a produce activity for publishing a message.
    /// </summary>
    public static Activity StartProduceActivity(
        string topic,
        string messageId,
        string messageKey = null,
        ActivityContext? parentContext = null)
    {
        var activity = Source.StartActivity(
            ProduceActivityName,
            ActivityKind.Producer,
            parentContext ?? default);

        if (activity != null)
        {
            activity.SetTag(MessagingSystemTag, "kafka");
            activity.SetTag(MessagingDestinationTag, topic);
            activity.SetTag(MessagingDestinationKindTag, "topic");
            activity.SetTag(MessagingOperationTag, "publish");
            activity.SetTag(MessagingMessageIdTag, messageId);

            if (messageKey != null)
            {
                activity.SetTag(MessagingKafkaMessageKeyTag, messageKey);
            }
        }

        return activity;
    }

    /// <summary>
    /// Creates a consume activity for receiving a message.
    /// </summary>
    public static Activity StartConsumeActivity(
        string topic,
        int partition,
        long offset,
        string consumerGroup,
        string messageId = null,
        string messageKey = null,
        ActivityContext? parentContext = null)
    {
        var activity = Source.StartActivity(
            ConsumeActivityName,
            ActivityKind.Consumer,
            parentContext ?? default);

        if (activity != null)
        {
            activity.SetTag(MessagingSystemTag, "kafka");
            activity.SetTag(MessagingDestinationTag, topic);
            activity.SetTag(MessagingDestinationKindTag, "topic");
            activity.SetTag(MessagingOperationTag, "receive");
            activity.SetTag(MessagingKafkaPartitionTag, partition.ToString());
            activity.SetTag(MessagingKafkaOffsetTag, offset.ToString());
            activity.SetTag(MessagingKafkaConsumerGroupTag, consumerGroup);

            if (messageId != null)
            {
                activity.SetTag(MessagingMessageIdTag, messageId);
            }

            if (messageKey != null)
            {
                activity.SetTag(MessagingKafkaMessageKeyTag, messageKey);
            }
        }

        return activity;
    }

    /// <summary>
    /// Creates a process activity for handling a message.
    /// </summary>
    public static Activity StartProcessActivity(
        string topic,
        string messageId,
        int partition,
        long offset,
        ActivityContext? parentContext = null)
    {
        var activity = Source.StartActivity(
            ProcessActivityName,
            ActivityKind.Consumer,
            parentContext ?? default);

        if (activity != null)
        {
            activity.SetTag(MessagingSystemTag, "kafka");
            activity.SetTag(MessagingDestinationTag, topic);
            activity.SetTag(MessagingOperationTag, "process");
            activity.SetTag(MessagingKafkaPartitionTag, partition.ToString());
            activity.SetTag(MessagingKafkaOffsetTag, offset.ToString());
            activity.SetTag(MessagingMessageIdTag, messageId);
        }

        return activity;
    }

    /// <summary>
    /// Records an exception on an activity.
    /// </summary>
    public static void RecordException(Activity activity, Exception exception)
    {
        if (activity == null || exception == null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        
        // Add exception details as an event (standard way without OpenTelemetry extensions)
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.StackTrace }
        };
        activity.AddEvent(new ActivityEvent("exception", tags: tags));
    }

    /// <summary>
    /// Marks an activity as successful.
    /// </summary>
    public static void SetSuccess(Activity activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
