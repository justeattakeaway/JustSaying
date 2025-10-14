using System.IO.Compression;

namespace JustSaying.Messaging.MessageSerialization;

internal static class DecompressionHelper
{
    /// <summary>
    /// Decompresses a message body if it is GZip + Base64 encoded, otherwise returns the original message body.
    /// </summary>
    /// <param name="messageBody">The message body to decompress.</param>
    /// <returns>The decompressed message body if it was compressed, otherwise the original message body.</returns>
    public static string ApplyDecompressionIfRequired(this string messageBody)
    {
        // check if message is compressed (GZip + Base64)
        // a message body will typically start with "{" if not compressed
        // a GZip + Base64 encoded message will be prefixed with "H4sI" regardless of the original message content
        if (!messageBody.StartsWith("H4sI"))
        {
            return messageBody;
        }
        byte[] gzipBuffer = Convert.FromBase64String(messageBody);
        using var compressedStream = new MemoryStream(gzipBuffer);
        using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        decompressionStream.CopyTo(resultStream);
        return System.Text.Encoding.UTF8.GetString(resultStream.ToArray());
    }
}
