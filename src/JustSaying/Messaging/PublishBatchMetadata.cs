using JustSaying.AwsTools;

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
}
