using System.Text;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// Provides utility methods for compressing messages based on specified criteria.
/// </summary>
internal static class MessageCompressionUtility
{
    /// <summary>
    /// Compresses a message if it meets the specified compression criteria.
    /// </summary>
    /// <param name="message">The original message to potentially compress.</param>
    /// <param name="metadata">Metadata associated with the message.</param>
    /// <param name="compressionOptions">Options specifying when and how to compress.</param>
    /// <param name="compressionRegistry">Registry of available compression algorithms.</param>
    /// <returns>A tuple containing the compressed message (or null if not compressed) and the content encoding used (or null if not compressed).</returns>
    public static (string compressedMessage, string contentEncoding) CompressMessageIfNeeded(string message, PublishMetadata metadata, PublishCompressionOptions compressionOptions, IMessageCompressionRegistry compressionRegistry)
    {
        string contentEncoding = null;
        string compressedMessage = null;
        if (compressionOptions?.CompressionEncoding is { } compressionEncoding && compressionRegistry is not null)
        {
            var messageSize = CalculateTotalMessageSize(message, metadata);
            if (messageSize > compressionOptions.MessageLengthThreshold)
            {
                var compression = GetCompressionAlgorithm(compressionEncoding, compressionRegistry);
                compressedMessage = compression.Compress(message);
                contentEncoding = compressionEncoding;
            }
        }

        return (compressedMessage, contentEncoding);
    }

    /// <summary>
    /// Calculates the total size of a message, including its metadata.
    /// </summary>
    /// <param name="message">The message content.</param>
    /// <param name="metadata">Metadata associated with the message.</param>
    /// <returns>The total size of the message in bytes.</returns>
    private static int CalculateTotalMessageSize(string message, PublishMetadata metadata)
    {
        var messageSize = Encoding.UTF8.GetByteCount(message);
        if (metadata?.MessageAttributes != null)
        {
            foreach (var attribute in metadata.MessageAttributes)
            {
                messageSize += Encoding.UTF8.GetByteCount(attribute.Key);
                messageSize += Encoding.UTF8.GetByteCount(attribute.Value.DataType);
                if (attribute.Value.StringValue is not null)
                {
                    messageSize += Encoding.UTF8.GetByteCount(attribute.Value.StringValue);
                }

                if (attribute.Value.BinaryValue is not null)
                {
                    messageSize += attribute.Value.BinaryValue.Count;
                }
            }
        }

        return messageSize;
    }

    /// <summary>
    /// Retrieves the compression algorithm for the specified encoding from the compression registry.
    /// </summary>
    /// <param name="compressionEncoding">The encoding of the desired compression algorithm.</param>
    /// <param name="compressionRegistry">The registry containing available compression algorithms.</param>
    /// <returns>The compression algorithm corresponding to the specified encoding.</returns>
    /// <exception cref="PublishException">Thrown when the specified compression encoding is not registered.</exception>
    private static IMessageBodyCompression GetCompressionAlgorithm(string compressionEncoding, IMessageCompressionRegistry compressionRegistry)
    {
        var compression = compressionRegistry.GetCompression(compressionEncoding);
        if (compression is null)
        {
            throw new PublishException($"Compression encoding '{compressionEncoding}' is not registered.");
        }
        return compression;
    }
}
