using System.IO.Compression;
using System.Text;

namespace JustSaying.Messaging.Compression;

/// <summary>
/// Implements GZIP compression and Base64 encoding for message bodies.
/// </summary>
public sealed class GzipMessageBodyCompression : IMessageBodyCompression
{
    /// <summary>
    /// Gets the content encoding identifier for GZIP compression with Base64 encoding.
    /// </summary>
    public string ContentEncoding => ContentEncodings.GzipBase64;

    /// <summary>
    /// Gets the content encoding identifier for GZIP compression with Base64 encoding.
    /// </summary>
    public string Compress(string messageBody)
    {
        var contentBytes = Encoding.UTF8.GetBytes(messageBody);
        using var compressedStream = new MemoryStream();
        using (var gZipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            gZipStream.Write(contentBytes, 0, contentBytes.Length);
        }

        return Convert.ToBase64String(compressedStream.ToArray());
    }

    /// <summary>
    /// Decodes the Base64 string and decompresses the message body using GZIP.
    /// </summary>
    /// <param name="messageBody">The Base64 encoded and compressed message body to decompress.</param>
    /// <returns>The decompressed message body as a string.</returns>
    public string Decompress(string messageBody)
    {
        var compressedBytes = Convert.FromBase64String(messageBody);
        using var inputStream = new MemoryStream(compressedBytes);
        using var outputStream = new MemoryStream();
        using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            gZipStream.CopyTo(outputStream);
        }

        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
