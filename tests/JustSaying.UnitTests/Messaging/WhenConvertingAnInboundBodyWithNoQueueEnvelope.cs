using JustSaying.AwsTools;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.UnitTests.Messaging;

/// <summary>
/// A publication whose serializer is self-describing (CloudEvents) is sent without the
/// <c>{Message, Subject}</c> queue envelope, and compressing it puts a bare Base64 string on the wire.
/// A subscriber left on the default (non-raw) delivery must still read both, rather than failing while
/// trying to parse the body as an envelope.
/// </summary>
public class WhenConvertingAnInboundBodyWithNoQueueEnvelope
{
    private sealed class OrderPlaced : Message
    {
        public string OrderId { get; set; }
    }

    private static readonly IMessageBodySerializer<OrderPlaced> Serializer =
        new SystemTextJsonMessageBodySerializer<OrderPlaced>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

    private static InboundMessageConverter CreateConverter()
        => new(
            Serializer.Erase(),
            new MessageCompressionRegistry([new GzipMessageBodyCompression()]),
            isRawMessage: false);

    [Test]
    public async Task AnUnwrappedJsonBodyIsPassedThrough()
    {
        var body = Serializer.Serialize(new OrderPlaced { OrderId = "order-1" });

        var result = await CreateConverter().ConvertToInboundMessageAsync(new Amazon.SQS.Model.Message { Body = body });

        result.Message.ShouldBeOfType<OrderPlaced>().OrderId.ShouldBe("order-1");
    }

    [Test]
    public async Task AnUnwrappedCompressedBodyIsDecompressed()
    {
        var compression = new GzipMessageBodyCompression();
        var body = compression.Compress(Serializer.Serialize(new OrderPlaced { OrderId = "order-2" }));

        var message = new Amazon.SQS.Model.Message
        {
            Body = body,
            MessageAttributes = new()
            {
                [MessageAttributeKeys.ContentEncoding] = new Amazon.SQS.Model.MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = compression.ContentEncoding,
                },
            },
        };

        var result = await CreateConverter().ConvertToInboundMessageAsync(message);

        result.Message.ShouldBeOfType<OrderPlaced>().OrderId.ShouldBe("order-2");
    }
}
