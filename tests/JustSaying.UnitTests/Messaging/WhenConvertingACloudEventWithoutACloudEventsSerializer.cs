using System.Text;
using System.Text.Json;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.UnitTests.Messaging;

/// <summary>
/// A serializer that doesn't understand CloudEvents reads a CloudEvent's attributes as the message's
/// properties, producing an all-default message. Such a message must fail (and be left for redrive)
/// rather than reach a handler.
/// </summary>
public class WhenConvertingACloudEventWithoutACloudEventsSerializer
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private sealed class SelfDescribingSerializer : IMessageBodySerializer<OrderPlaced>, ISelfDescribingMessageBodySerializer
    {
        public string Serialize(OrderPlaced message) => throw new NotSupportedException();

        public OrderPlaced Deserialize(string message) => new() { OrderId = "read-by-the-cloudevents-serializer" };
    }

    private const string CloudEvent =
        """{"specversion":"1.0","id":"evt-1","source":"/orders","type":"com.example.order.placed","data":{"OrderId":"order-1"}}""";

    private static InboundMessageConverter CreateConverter(IMessageBodySerializer<OrderPlaced> serializer = null, bool isRawMessage = false)
        => new(
            (serializer ?? new SystemTextJsonMessageBodySerializer<OrderPlaced>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)).Erase(),
            new MessageCompressionRegistry(),
            isRawMessage);

    private static string SnsNotification(string innerBody)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Type", "Notification");
            writer.WriteString("Subject", "OrderPlaced");
            writer.WriteString("Message", innerBody);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task A_CloudEvent_Sent_To_A_Queue_Fails(bool isRawMessage)
    {
        var converter = CreateConverter(isRawMessage: isRawMessage);

        var exception = await Should.ThrowAsync<UnroutableMessageException>(
            async () => await converter.ConvertToInboundMessageAsync(new Amazon.SQS.Model.Message { Body = CloudEvent }));

        exception.Message.ShouldContain("CloudEvent (type 'com.example.order.placed')");
        exception.Message.ShouldContain("WithMessageBodySerializer");
    }

    [Test]
    public async Task A_CloudEvent_Delivered_By_Sns_Fails()
    {
        var converter = CreateConverter();

        await Should.ThrowAsync<UnroutableMessageException>(
            async () => await converter.ConvertToInboundMessageAsync(new Amazon.SQS.Model.Message { Body = SnsNotification(CloudEvent) }));
    }

    [Test]
    public async Task A_CloudEvent_Is_Read_By_A_Self_Describing_Serializer()
    {
        var converter = CreateConverter(new SelfDescribingSerializer());

        var result = await converter.ConvertToInboundMessageAsync(new Amazon.SQS.Model.Message { Body = CloudEvent });

        result.Message.ShouldBeOfType<OrderPlaced>().OrderId.ShouldBe("read-by-the-cloudevents-serializer");
    }

    [Test]
    public async Task A_Message_That_Only_Mentions_Specversion_Is_Read_Normally()
    {
        // Missing the required id, source and type, so not a CloudEvent.
        var converter = CreateConverter();

        var result = await converter.ConvertToInboundMessageAsync(
            new Amazon.SQS.Model.Message { Body = SnsNotification("""{"OrderId":"order-2","specversion":"1.0"}""") });

        result.Message.ShouldBeOfType<OrderPlaced>().OrderId.ShouldBe("order-2");
    }
}
