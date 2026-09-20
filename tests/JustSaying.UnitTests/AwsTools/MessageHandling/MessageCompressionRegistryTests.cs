using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.AwsTools.MessageHandling;

public class MessageCompressionRegistryTests
{
    private readonly MessageCompressionRegistry _compressionRegistry = new([new GzipMessageBodyCompression()]);
    private readonly SystemTextJsonMessageBodySerializer<SimpleMessage> _bodySerializer = new(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

    private OutboundMessageConverter CreateConverter(
        PublishCompressionOptions compressionOptions,
        PublishDestinationType destinationType = PublishDestinationType.Topic,
        bool isRawMessage = false,
        int? maximumMessageSize = null)
        => new(
            destinationType,
            _bodySerializer,
            _compressionRegistry,
            compressionOptions,
            "TestSubject",
            isRawMessage,
            maximumMessageSize ?? JustSayingConstants.DefaultSnsMaximumMessageSize);

    private static bool IsCompressed(OutboundMessage outboundMessage)
        => outboundMessage.MessageAttributes.ContainsKey(MessageAttributeKeys.ContentEncoding);

    [Test]
    public async Task NoCompressionEncoding_DoesNotCompress()
    {
        var message = new SimpleMessage { Content = new string('a', 1000) };
        var converter = CreateConverter(new PublishCompressionOptions());

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeFalse();
    }

    [Test]
    public async Task ThresholdNotMet_DoesNotCompress()
    {
        var message = new SimpleMessage { Content = "Short message" };
        var converter = CreateConverter(new PublishCompressionOptions
        {
            CompressionEncoding = ContentEncodings.GzipBase64,
            MessageLengthThreshold = 1000
        });

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeFalse();
    }

    [Test]
    public async Task ThresholdMet_CompressesAndRoundTrips()
    {
        var message = new SimpleMessage { Content = new string('a', 1000) };
        var converter = CreateConverter(new PublishCompressionOptions
        {
            CompressionEncoding = ContentEncodings.GzipBase64,
            MessageLengthThreshold = 500
        });

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeTrue();
        result.MessageAttributes[MessageAttributeKeys.ContentEncoding].StringValue.ShouldBe(ContentEncodings.GzipBase64);

        var decompressed = new GzipMessageBodyCompression().Decompress(result.Body);
        decompressed.ShouldBe(_bodySerializer.Serialize(message));
    }

    [Test]
    public async Task ThresholdMet_WithRawQueueMessage_CompressesAndRoundTrips()
    {
        var message = new SimpleMessage { Content = new string('a', 1000) };
        var converter = CreateConverter(
            new PublishCompressionOptions
            {
                CompressionEncoding = ContentEncodings.GzipBase64,
                MessageLengthThreshold = 500
            },
            PublishDestinationType.Queue,
            isRawMessage: true,
            JustSayingConstants.DefaultSqsMaximumMessageSize);

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeTrue();

        var decompressed = new GzipMessageBodyCompression().Decompress(result.Body);
        decompressed.ShouldBe(_bodySerializer.Serialize(message));
    }

    [Test]
    public async Task MessageAttributes_CountTowardsTheThreshold()
    {
        // The body alone sits under the threshold, the attributes are what push it over. The body is
        // compressible so that compression is still a win once it kicks in.
        var message = new SimpleMessage { Content = new string('a', 400) };
        var metadata = new PublishMetadata();
        metadata.AddMessageAttribute("Key1", new MessageAttributeValue { StringValue = new string('v', 200), DataType = "String" });
        metadata.AddMessageAttribute("Key2", new MessageAttributeValue { BinaryValue = new byte[200], DataType = "Binary" });

        var options = new PublishCompressionOptions
        {
            CompressionEncoding = ContentEncodings.GzipBase64,
            MessageLengthThreshold = 600
        };

        // Without the attributes the same message stays under the threshold.
        var withoutAttributes = await CreateConverter(options).ConvertToOutboundMessageAsync(message, new PublishMetadata());
        IsCompressed(withoutAttributes).ShouldBeFalse();

        var withAttributes = await CreateConverter(options).ConvertToOutboundMessageAsync(message, metadata);
        IsCompressed(withAttributes).ShouldBeTrue();
    }

    [Test]
    public async Task CompressionIsSkipped_WhenItWouldMakeTheMessageLarger()
    {
        // A short body still exceeds a tiny threshold, but gzip plus base64 inflates it, so the
        // uncompressed body should win.
        var message = new SimpleMessage { Content = "a" };
        var converter = CreateConverter(new PublishCompressionOptions
        {
            CompressionEncoding = ContentEncodings.GzipBase64,
            MessageLengthThreshold = 1
        });

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeFalse();
        result.Body.ShouldBe(_bodySerializer.Serialize(message));
    }

    [Test]
    public async Task DefaultThreshold_IsDerivedFromTheDestinationMaximum()
    {
        // 300 KiB of highly compressible content: over the SNS default threshold (254 KiB), but
        // under the SQS one (1022 KiB).
        var message = new SimpleMessage { Content = new string('a', 300 * 1024) };
        var options = new PublishCompressionOptions { CompressionEncoding = ContentEncodings.GzipBase64 };

        var toTopic = await CreateConverter(options, maximumMessageSize: JustSayingConstants.DefaultSnsMaximumMessageSize)
            .ConvertToOutboundMessageAsync(message, new PublishMetadata());

        var toQueue = await CreateConverter(options, PublishDestinationType.Queue, isRawMessage: true, JustSayingConstants.DefaultSqsMaximumMessageSize)
            .ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(toTopic).ShouldBeTrue();
        IsCompressed(toQueue).ShouldBeFalse();
    }

    [Test]
    public async Task RaisingTheTopicMaximum_RaisesTheDefaultThreshold()
    {
        var message = new SimpleMessage { Content = new string('a', 300 * 1024) };
        var options = new PublishCompressionOptions { CompressionEncoding = ContentEncodings.GzipBase64 };

        var converter = CreateConverter(options, maximumMessageSize: JustSayingConstants.MaximumSnsMessageSize);

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeFalse();
    }

    /// <summary>
    /// The threshold is about when compressing is worthwhile, so one set above the destination's limit
    /// (say, shared options tuned for a 1 MiB queue, used against a 256 KiB topic) must not stop a message
    /// that would otherwise be rejected from being compressed.
    /// </summary>
    [Test]
    public async Task ThresholdAboveTheDestinationMaximum_StillCompressesAMessageThatWouldNotFit()
    {
        var message = new SimpleMessage { Content = new string('a', 300 * 1024) };
        var converter = CreateConverter(new PublishCompressionOptions
        {
            CompressionEncoding = ContentEncodings.GzipBase64,
            MessageLengthThreshold = 1000 * 1024
        });

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeTrue();
    }

    [Test]
    public async Task ThresholdAboveTheDestinationMaximum_DoesNotCompressAMessageThatFits()
    {
        var message = new SimpleMessage { Content = new string('a', 100 * 1024) };
        var converter = CreateConverter(new PublishCompressionOptions
        {
            CompressionEncoding = ContentEncodings.GzipBase64,
            MessageLengthThreshold = 1000 * 1024
        });

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeFalse();
    }

    /// <summary>
    /// A queue can have its MaximumMessageSize set below the 1 MiB SQS default, and the default threshold
    /// has to follow it down.
    /// </summary>
    [Test]
    public async Task LoweringTheQueueMaximum_LowersTheDefaultThreshold()
    {
        var message = new SimpleMessage { Content = new string('a', 300 * 1024) };
        var options = new PublishCompressionOptions { CompressionEncoding = ContentEncodings.GzipBase64 };

        var converter = CreateConverter(options, PublishDestinationType.Queue, isRawMessage: true, maximumMessageSize: 256 * 1024);

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        IsCompressed(result).ShouldBeTrue();
    }

    /// <summary>
    /// Compression runs before the queue envelope is applied, so it must not try to unwrap the body.
    /// A message type with its own top-level "Message" property used to have its body replaced by that
    /// property's value on the way out.
    /// </summary>
    [Test]
    public async Task CompressingAQueueMessage_DoesNotUnwrapAMessageProperty()
    {
        var serializer = new SystemTextJsonMessageBodySerializer<MessageWithMessageProperty>(
            SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

        var message = new MessageWithMessageProperty { Message = new string('a', 1000) };

        var converter = new OutboundMessageConverter(
            PublishDestinationType.Queue,
            serializer,
            _compressionRegistry,
            new PublishCompressionOptions
            {
                CompressionEncoding = ContentEncodings.GzipBase64,
                MessageLengthThreshold = 500
            },
            "TestSubject",
            isRawMessage: false,
            JustSayingConstants.DefaultSqsMaximumMessageSize);

        var result = await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata());

        result.MessageAttributes.ShouldContainKey(MessageAttributeKeys.ContentEncoding);

        // The envelope wraps the compressed body, so unwrap then decompress.
        var envelope = System.Text.Json.Nodes.JsonNode.Parse(result.Body);
        var decompressed = new GzipMessageBodyCompression().Decompress(envelope["Message"].GetValue<string>());

        decompressed.ShouldBe(serializer.Serialize(message));
    }

    private class MessageWithMessageProperty : JustSaying.Models.Message
    {
        public string Message { get; set; }
    }

    [Test]
    public async Task MessageOverTheDestinationMaximum_Throws()
    {
        // 1 MiB of high-entropy content: gzip plus base64 cannot bring this under the 256 KiB the
        // topic accepts.
        var random = new Random(42);
        var content = new char[1024 * 1024];
        for (int i = 0; i < content.Length; i++)
        {
            content[i] = (char)random.Next('a', 'z' + 1);
        }

        var message = new SimpleMessage { Content = new string(content) };
        var converter = CreateConverter(new PublishCompressionOptions
        {
            CompressionEncoding = ContentEncodings.GzipBase64
        });

        var exception = await Should.ThrowAsync<MessageTooLargeException>(
            async () => await converter.ConvertToOutboundMessageAsync(message, new PublishMetadata()));

        exception.MaximumMessageSize.ShouldBe(JustSayingConstants.DefaultSnsMaximumMessageSize);
        exception.MessageSize.ShouldBeGreaterThan(JustSayingConstants.DefaultSnsMaximumMessageSize);
    }
}
