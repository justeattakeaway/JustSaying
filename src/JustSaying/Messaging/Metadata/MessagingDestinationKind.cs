namespace JustSaying.Messaging.Metadata;

/// <summary>
/// The kind of AWS destination a publication or subscription is bound to.
/// </summary>
public enum MessagingDestinationKind
{
    /// <summary>
    /// An SNS topic.
    /// </summary>
    SnsTopic,

    /// <summary>
    /// An SQS queue.
    /// </summary>
    SqsQueue,
}
