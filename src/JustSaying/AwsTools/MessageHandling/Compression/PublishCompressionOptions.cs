using JustSaying.Messaging.Compression;

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// Represents options for message compression during publishing.
/// </summary>
public sealed class PublishCompressionOptions
{
    /// <summary>
    /// Gets or sets the message length threshold in bytes.
    /// Messages at or above this threshold will be compressed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This controls when compressing is considered worthwhile, which is a separate question from how large
    /// a message the destination will accept. Lower it to trade CPU for smaller payloads and lower cost.
    /// </para>
    /// <para>
    /// When <see langword="null"/> (the default), the threshold is derived from the destination's maximum
    /// message size, leaving <see cref="JustSayingConstants.DefaultCompressionHeadroom"/> of headroom. That
    /// works out at 254 KiB for an SNS topic using the default 256 KiB limit, and 1022 KiB for an SQS queue.
    /// </para>
    /// </remarks>
    public int? MessageLengthThreshold { get; set; }

    /// <summary>
    /// Gets or sets the compression encoding to be used.
    /// </summary>
    /// <remarks>
    /// This should correspond to a registered compression algorithm in the <see cref="MessageCompressionRegistry"/>.
    /// </remarks>
    public string CompressionEncoding { get; set; }

    /// <summary>
    /// Gets the threshold, in bytes, at or above which a message published to a destination with the
    /// supplied maximum message size should be compressed.
    /// </summary>
    /// <param name="maximumMessageSize">The maximum message size the destination accepts, in bytes.</param>
    internal int GetThresholdFor(int maximumMessageSize)
        => MessageLengthThreshold ?? Math.Max(maximumMessageSize - JustSayingConstants.DefaultCompressionHeadroom, 1);
}
