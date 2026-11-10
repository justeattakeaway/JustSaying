using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Fluent;

internal static class PublicationCompression
{
    /// <summary>
    /// Resolves the compression options for a publication: its own, else the bus default. A self-describing
    /// body (such as a CloudEvent) is meant to be read by consumers that know nothing of JustSaying's
    /// compression, so it never takes the bus default, and compressing one explicitly is a configuration error.
    /// </summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="configured">The compression options configured on the publication, if any.</param>
    /// <param name="busDefault">The bus default compression options, if the publication takes them.</param>
    /// <param name="isSelfDescribing">Whether the publication's serializer is self-describing.</param>
    public static PublishCompressionOptions Resolve<T>(PublishCompressionOptions configured, PublishCompressionOptions busDefault, bool isSelfDescribing)
    {
        if (!isSelfDescribing)
        {
            return configured ?? busDefault;
        }

        if (configured?.CompressionEncoding is { } compressionEncoding)
        {
            throw new InvalidOperationException(
                $"The publication for '{typeof(T).Name}' is configured to compress messages with '{compressionEncoding}', but its serializer is " +
                "self-describing (for example CloudEvents), and a compressed body can't be read by consumers that don't use JustSaying. " +
                "Remove the compression from this publication.");
        }

        return null;
    }
}
