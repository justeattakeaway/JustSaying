using JustSaying.Messaging.Compression;

namespace JustSaying.UnitTests.Messaging.Compression
{
    public class GzipMessageBodyCompressionTests
    {
        private readonly GzipMessageBodyCompression _compression = new();

        [Test]
        public void ContentEncoding_ShouldReturnGzipBase64()
        {
            _compression.ContentEncoding.ShouldBe(ContentEncodings.GzipBase64);
        }

        [Test]
        [Arguments("")]
        [Arguments("Hello, World!")]
        [Arguments("This is a longer string with some special characters: !@#$%^&*()_+")]
        public void Compress_ThenDecompress_ShouldReturnOriginalString(string original)
        {
            // Arrange

            // Act
            string compressed = _compression.Compress(original);
            string decompressed = _compression.Decompress(compressed);

            // Assert
            decompressed.ShouldBe(original);
        }

        [Test]
        public void Compress_ShouldReturnBase64EncodedString()
        {
            // Arrange
            string input = "Test string";

            // Act
            string compressed = _compression.Compress(input);

            // Assert
            IsBase64String(compressed).ShouldBeTrue();
        }

        [Test]
        public void Decompress_WithInvalidBase64_ShouldThrowFormatException()
        {
            // Arrange
            string invalidBase64 = "This is not a valid Base64 string";

            // Act & Assert
            Should.Throw<FormatException>(() => _compression.Decompress(invalidBase64));
        }

        [Test]
        public void Compress_WithLargeString_ShouldCompressSuccessfully()
        {
            // Arrange
            string largeString = new string('A', 1_000_000);

            // Act
            string compressed = _compression.Compress(largeString);
            string decompressed = _compression.Decompress(compressed);

            // Assert
            decompressed.ShouldBe(largeString);
        }

        private bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int _);
        }
    }
}
