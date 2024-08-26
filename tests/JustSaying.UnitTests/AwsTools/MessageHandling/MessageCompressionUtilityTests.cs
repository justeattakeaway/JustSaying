// using JustSaying.Messaging;
// using JustSaying.Messaging.Compression;
// using JustSaying.AwsTools.MessageHandling;
//
// namespace JustSaying.UnitTests.AwsTools.MessageHandling;
//
// public class MessageCompressionUtilityTests
// {
//     private readonly MessageCompressionRegistry _compressionRegistry;
//
//     public MessageCompressionUtilityTests()
//     {
//         _compressionRegistry = new MessageCompressionRegistry([new GzipMessageBodyCompression()]);
//     }
//
//     [Fact]
//     public void CompressMessageIfNeeded_NoCompression_ReturnsOriginalMessage()
//     {
//         // Arrange
//         var message = "Test message";
//         var metadata = new PublishMetadata();
//         var compressionOptions = new PublishCompressionOptions();
//
//         // Act
//         var result = MessageCompressionUtility.CompressMessageBody(message, metadata, PublishDestinationType.Topic, compressionOptions, _compressionRegistry);
//
//         // Assert
//         Assert.Null(result.compressedMessage);
//         Assert.Null(result.contentEncoding);
//     }
//
//     [Fact]
//     public void CompressMessageIfNeeded_CompressionThresholdNotMet_ReturnsOriginalMessage()
//     {
//         // Arrange
//         var message = "Short message";
//         var metadata = new PublishMetadata();
//         var compressionOptions = new PublishCompressionOptions
//         {
//             CompressionEncoding = ContentEncodings.GzipBase64,
//             MessageLengthThreshold = 1000
//         };
//
//         // Act
//         var result = MessageCompressionUtility.CompressMessageBody(message, metadata, PublishDestinationType.Topic, compressionOptions, _compressionRegistry);
//
//         // Assert
//         Assert.Null(result.compressedMessage);
//         Assert.Null(result.contentEncoding);
//     }
//
//     [Fact]
//     public void CompressMessageIfNeeded_CompressionThresholdMet_ReturnsCompressedMessage()
//     {
//         // Arrange
//         var message = new string('a', 1000);
//         var metadata = new PublishMetadata();
//         var compressionOptions = new PublishCompressionOptions
//         {
//             CompressionEncoding = ContentEncodings.GzipBase64,
//             MessageLengthThreshold = 500
//         };
//
//         // Act
//         var result = MessageCompressionUtility.CompressMessageBody(message, metadata, PublishDestinationType.Topic, compressionOptions, _compressionRegistry);
//
//         // Assert
//         Assert.NotNull(result.compressedMessage);
//         Assert.Equal(ContentEncodings.GzipBase64, result.contentEncoding);
//
//         // Verify that the compressed message can be decompressed
//         var gzipCompression = new GzipMessageBodyCompression();
//         var decompressedMessage = gzipCompression.Decompress(result.compressedMessage);
//         Assert.Equal(message, decompressedMessage);
//     }
//
//     [Fact]
//     public void CompressMessageIfNeeded_WithMessageAttributes_CalculatesTotalSize()
//     {
//         // Arrange
//         var message = "Test message";
//         var metadata = new PublishMetadata();
//         metadata.AddMessageAttribute("Key1", new MessageAttributeValue { StringValue = "Value1", DataType = "String" });
//         metadata.AddMessageAttribute("Key2", new MessageAttributeValue { BinaryValue = new byte[100], DataType = "Binary" });
//
//         var compressionOptions = new PublishCompressionOptions
//         {
//             CompressionEncoding = ContentEncodings.GzipBase64,
//             MessageLengthThreshold = 50
//         };
//
//         // Act
//         var result = MessageCompressionUtility.CompressMessageBody(message, metadata, PublishDestinationType.Topic, compressionOptions, _compressionRegistry);
//
//         // Assert
//         Assert.NotNull(result.compressedMessage);
//         Assert.Equal(ContentEncodings.GzipBase64, result.contentEncoding);
//
//         // Verify that the compressed message can be decompressed
//         var gzipCompression = new GzipMessageBodyCompression();
//         var decompressedMessage = gzipCompression.Decompress(result.compressedMessage);
//         Assert.Equal(message, decompressedMessage);
//     }
// }

// TODO
