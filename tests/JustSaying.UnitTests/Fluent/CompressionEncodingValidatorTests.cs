using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
using JustSaying.Messaging.Compression;

namespace JustSaying.UnitTests.Fluent;

public class CompressionEncodingValidatorTests
{
    [Fact]
    public void ValidateEncoding_WithNullCompressionOptions_DoesNotThrow()
    {
        // Arrange
        var registry = new MessageCompressionRegistry();

        // Act & Assert
        CompressionEncodingValidator.ValidateEncoding(registry, null);
    }

    [Fact]
    public void ValidateEncoding_WithNullCompressionEncoding_DoesNotThrow()
    {
        // Arrange
        var registry = new MessageCompressionRegistry();
        var options = new PublishCompressionOptions { CompressionEncoding = null };

        // Act & Assert
        CompressionEncodingValidator.ValidateEncoding(registry, options);
    }

    [Fact]
    public void ValidateEncoding_WithRegisteredEncoding_DoesNotThrow()
    {
        // Arrange
        var gzipCompression = new GzipMessageBodyCompression();
        var registry = new MessageCompressionRegistry([gzipCompression]);
        var options = new PublishCompressionOptions { CompressionEncoding = ContentEncodings.GzipBase64 };

        // Act & Assert
        CompressionEncodingValidator.ValidateEncoding(registry, options);
    }

    [Fact]
    public void ValidateEncoding_WithUnregisteredEncoding_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new MessageCompressionRegistry();
        var options = new PublishCompressionOptions { CompressionEncoding = "unknown" };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CompressionEncodingValidator.ValidateEncoding(registry, options));

        Assert.Equal("Compression encoding 'unknown' is not registered with the bus.", exception.Message);
    }
}
