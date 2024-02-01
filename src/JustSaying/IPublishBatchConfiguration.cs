using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;

namespace JustSaying;

/// <summary>
/// Defines the configuration for publishing batches of messages.
/// </summary>
public interface IPublishBatchConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of re-publish attempts to make.
    /// </summary>
    int PublishFailureReAttempts { get; set; }

    /// <summary>
    /// Gets or sets the amount of time to wait before retrying a failed publish.
    /// </summary>
    TimeSpan PublishFailureBackoff { get; set; }

    /// <summary>
    /// Gets or sets a delegate to log when a message batch is published.
    /// </summary>
    Action<MessageBatchResponse, IReadOnlyCollection<Message>> MessageBatchResponseLogger { get; set; }
}
