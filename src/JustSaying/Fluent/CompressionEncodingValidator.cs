using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Compression;

namespace JustSaying.Fluent;

internal static class CompressionEncodingValidator
{
    public static void ValidateEncoding(MessageCompressionRegistry compressionRegistry, PublishCompressionOptions compressionOptions)
    {
        if (compressionOptions?.CompressionEncoding is { } compressionEncoding)
        {
            if (compressionRegistry.GetCompression(compressionEncoding) is null)
            {
                throw new InvalidOperationException($"Compression encoding '{compressionEncoding}' is not registered with the bus.");
            }
        }
    }
}
