using System.Text;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;

namespace JustSaying.AwsTools.MessageHandling;

internal static class MessageCompressionUtility
{
    public static (string compressedMessage, string contentEncoding) CompressMessageIfNeeded(string message, PublishMetadata metadata, PublishCompressionOptions compressionOptions, IMessageCompressionRegistry compressionRegistry)
    {
        string contentEncoding = null;
        string compressedMessage = null;
        if (compressionOptions?.CompressionEncoding is { } compressionEncoding && compressionRegistry is not null)
        {
            var messageSize = CalculateTotalMessageSize(metadata, message);
            if (messageSize > compressionOptions.MessageLengthThreshold)
            {
                var compression = GetCompressionAlgorithm(compressionEncoding, compressionRegistry);
                compressedMessage = compression.Compress(message);
                contentEncoding = compressionEncoding;
            }
        }
        
        return (compressedMessage, contentEncoding);
    }

    private static int CalculateTotalMessageSize(PublishMetadata metadata, string message)
    {
        var messageSize = Encoding.UTF8.GetByteCount(message);
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
        return messageSize;
    }

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
