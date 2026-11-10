using JustSaying.AwsTools.MessageHandling;

namespace JustSaying;

/// <summary>
/// The configuration for publishing batches of messages, kept separate from the single-message
/// <see cref="IMessagingConfig"/> so that batch-specific settings don't overwrite it.
/// </summary>
internal sealed class PublishBatchConfiguration : IPublishBatchConfiguration
{
    public int PublishFailureReAttempts { get; set; }

    public TimeSpan PublishFailureBackoff { get; set; }

    public Action<MessageBatchResponse, IReadOnlyCollection<object>> MessageBatchResponseLogger { get; set; }
}
