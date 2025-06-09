using JustSaying.AwsTools;
using JustSaying.Models;

namespace JustSaying.Messaging;

/// <summary>
/// A class representing publish metadata for a batch of messages.
/// </summary>
public class PublishBatchMetadata : PublishMetadata
{
    /// <summary>
    /// Gets or sets the batch size to use to publish messages.
    /// </summary>
    /// <remarks>
    /// The default value is the value of <see cref="JustSayingConstants.MaximumSnsBatchSize"/>.
    /// </remarks>
    public int BatchSize { get; set; } = JustSayingConstants.MaximumSnsBatchSize;

    /// <summary>
    /// Gets or sets the per-message MessageGroupIds.
    /// </summary>
    public Dictionary<Message, string> MessageGroupIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the per-message MessageDeduplicationIds.
    /// </summary>
    public Dictionary<Message, string> MessageDeduplicationIds { get; set; } = [];

    public PublishMetadata AddMessageGroupId(Message message, string messageGroupId)
    {
        MessageGroupIds[message] = messageGroupId;
        return this;
    }

    public PublishMetadata AddMessageDeduplicationId(Message message, string messageDeduplicationId)
    {
        MessageDeduplicationIds[message] = messageDeduplicationId;
        return this;
    }
}
