namespace JustSaying.Messaging.Compression;

/// <summary>
/// Defines the contract for a registry of message compression methods.
/// </summary>
public interface IMessageCompressionRegistry
{
    /// <summary>
    /// Retrieves the appropriate compression method based on the content encoding.
    /// </summary>
    /// <param name="contentEncoding">The content encoding identifier.</param>
    /// <returns>An <see cref="IMessageBodyCompression"/> instance for the specified encoding, or null if not found.</returns>
    IMessageBodyCompression GetCompression(string contentEncoding);
}
