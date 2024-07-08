using JustSaying.Messaging.Compression;

namespace JustSaying.UnitTests.Messaging.Compression
{
    public class GzipMessageBodyCompressionTests
    {
        private readonly GzipMessageBodyCompression _compression = new();

        [Fact]
        public void ContentEncoding_ShouldReturnGzipBase64()
        {
            Assert.Equal(ContentEncodings.GzipBase64, _compression.ContentEncoding);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Hello, World!")]
        [InlineData("This is a longer string with some special characters: !@#$%^&*()_+")]
        public void Compress_ThenDecompress_ShouldReturnOriginalString(string original)
        {
            // Arrange

            // Act
            string compressed = _compression.Compress(original);
            string decompressed = _compression.Decompress(compressed);

            // Assert
            Assert.Equal(original, decompressed);
        }

        [Fact]
        public void Compress_ShouldReturnBase64EncodedString()
        {
            // Arrange
            string input = "Test string";

            // Act
            string compressed = _compression.Compress(input);

            // Assert
            Assert.True(IsBase64String(compressed));
        }

        [Fact]
        public void Decompress_WithInvalidBase64_ShouldThrowFormatException()
        {
            // Arrange
            string invalidBase64 = "This is not a valid Base64 string";

            // Act & Assert
            Assert.Throws<FormatException>(() => _compression.Decompress(invalidBase64));
        }

        [Fact]
        public void Compress_WithLargeString_ShouldCompressSuccessfully()
        {
            // Arrange
            string largeString = new string('A', 1000000);  // 1 million 'A' characters

            // Act
            string compressed = _compression.Compress(largeString);
            string decompressed = _compression.Decompress(compressed);

            // Assert
            Assert.Equal(largeString, decompressed);
        }

        private bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int _);
        }
    }
}
