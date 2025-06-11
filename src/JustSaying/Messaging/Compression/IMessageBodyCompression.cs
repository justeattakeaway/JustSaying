namespace JustSaying.Messaging.Compression;

/// <summary>
/// Defines the contract for message body compression operations.
/// </summary>
public interface IMessageBodyCompression
{
    /// <summary>
    /// Gets the content encoding identifier for this compression method.
    /// </summary>
    string ContentEncoding { get; }

    /// <summary>
    /// Compresses the given message body.
    /// </summary>
    /// <param name="messageBody">The message body to compress.</param>
    /// <returns>The compressed message body as a string.</returns>
    string Compress(string messageBody);

    /// <summary>
    /// Decompresses the given message body.
    /// </summary>
    /// <param name="messageBody">The compressed message body to decompress.</param>
    /// <returns>The decompressed message body as a string.</returns>
    string Decompress(string messageBody);
}
