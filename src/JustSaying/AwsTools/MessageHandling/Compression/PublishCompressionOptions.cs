using JustSaying.Messaging.Compression;

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// Represents options for message compression during publishing.
/// </summary>
public sealed class PublishCompressionOptions
{
    /// <summary>
    /// Gets or sets the message length threshold in bytes.
    /// Messages larger than this threshold will be compressed.
    /// </summary>
    /// <remarks>
    /// The default value is 260,096 bytes (254 KB), 2KB less than the SNS and SQS limit.
    /// </remarks>
    public int MessageLengthThreshold { get; set; } = 254 * 1024;

    /// <summary>
    /// Gets or sets the compression encoding to be used.
    /// </summary>
    /// <remarks>
    /// This should correspond to a registered compression algorithm in the <see cref="MessageCompressionRegistry"/>.
    /// </remarks>
    public string CompressionEncoding { get; set; }
}
