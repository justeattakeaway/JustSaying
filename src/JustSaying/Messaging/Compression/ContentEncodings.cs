namespace JustSaying.Messaging.Compression;

/// <summary>
/// Provides constant values for various content encodings used in message body compression.
/// </summary>
/// <remarks>
/// This class contains predefined string constants representing different content encoding schemes.
/// These constants can be used to ensure consistency and avoid typos when specifying
/// content encodings throughout the application.
/// </remarks>
public static class ContentEncodings
{
    /// <summary>
    /// The GzipBase64 constant represents the "gzip,base64" content encoding.
    /// </summary>
    public const string GzipBase64 = "gzip,base64";
}
