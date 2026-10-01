using System.Text;
using System.Text.Json;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.UnitTests.Messaging;

public class WhenDiscriminatingInboundMessageTypes
{
    private sealed class OrderPlaced : Message
    {
        public string OrderId { get; set; }
    }

    private sealed class OrderShipped : Message
    {
        public string TrackingId { get; set; }
    }

    private static InboundMessageConverter CreateMultiTypeConverter()
    {
        var options = SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions;
        var serializersByName = new Dictionary<string, IMessageBodySerializer>(StringComparer.Ordinal)
        {
            ["OrderPlaced"] = new SystemTextJsonMessageBodySerializer<OrderPlaced>(options).Erase(),
            ["OrderShipped"] = new SystemTextJsonMessageBodySerializer<OrderShipped>(options).Erase(),
        };

        var resolver = new DiscriminatingInboundMessageSerializerResolver(
            [new SubjectMessageTypeDiscriminator()],
            serializersByName);

        return new InboundMessageConverter(resolver, new MessageCompressionRegistry(), isRawMessage: false);
    }

    private static Amazon.SQS.Model.Message SnsMessage(string subject, string innerBody)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Type", "Notification");
            writer.WriteString("Subject", subject);
            writer.WriteString("Message", innerBody);
            writer.WriteEndObject();
        }

        return new Amazon.SQS.Model.Message { Body = Encoding.UTF8.GetString(stream.ToArray()) };
    }

    [Test]
    public async Task EachMessageIsDeserializedIntoTheTypeMatchingItsSubject()
    {
        var converter = CreateMultiTypeConverter();

        var placedBody = new SystemTextJsonMessageBodySerializer<OrderPlaced>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
            .Serialize(new OrderPlaced { OrderId = "order-1" });
        var shippedBody = new SystemTextJsonMessageBodySerializer<OrderShipped>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
            .Serialize(new OrderShipped { TrackingId = "track-9" });

        var placed = await converter.ConvertToInboundMessageAsync(SnsMessage("OrderPlaced", placedBody));
        var shipped = await converter.ConvertToInboundMessageAsync(SnsMessage("OrderShipped", shippedBody));

        placed.Message.ShouldBeOfType<OrderPlaced>().OrderId.ShouldBe("order-1");
        shipped.Message.ShouldBeOfType<OrderShipped>().TrackingId.ShouldBe("track-9");
    }

    [Test]
    public async Task AnUnrecognisedSubjectIsUnroutableAndTheErrorSaysWhatWasFound()
    {
        var converter = CreateMultiTypeConverter();

        var exception = await Should.ThrowAsync<UnroutableMessageException>(
            async () => await converter.ConvertToInboundMessageAsync(SnsMessage("SomethingElse", "{\"Secret\":\"do-not-log\"}")));

        exception.Message.ShouldContain("subject 'SomethingElse'");
        exception.Message.ShouldContain("'OrderPlaced'");
        exception.Message.ShouldContain("'OrderShipped'");
        exception.Message.ShouldNotContain("do-not-log");
    }

    [Test]
    public async Task AMessageWithNoSubjectIsUnroutable()
    {
        var converter = CreateMultiTypeConverter();
        var raw = new Amazon.SQS.Model.Message { Body = "{\"OrderId\":\"order-1\"}" };

        var exception = await Should.ThrowAsync<UnroutableMessageException>(
            async () => await converter.ConvertToInboundMessageAsync(raw));

        exception.Message.ShouldContain("subject: none");
    }

    [Test]
    public async Task AMessageRecognisedByAnEarlierDiscriminatorDoesNotFallThroughToTheSubject()
    {
        // The first discriminator recognises the message as a type the queue doesn't handle; the SNS
        // Subject happens to name a registered type, but must not be used to read the message as it.
        var options = SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions;
        var resolver = new DiscriminatingInboundMessageSerializerResolver(
            [new FixedTypeDiscriminator("com.example.unknown"), new SubjectMessageTypeDiscriminator()],
            new Dictionary<string, IMessageBodySerializer>(StringComparer.Ordinal)
            {
                ["OrderPlaced"] = new SystemTextJsonMessageBodySerializer<OrderPlaced>(options).Erase(),
            });
        var converter = new InboundMessageConverter(resolver, new MessageCompressionRegistry(), isRawMessage: false);

        await Should.ThrowAsync<MessageFormatNotSupportedException>(
            async () => await converter.ConvertToInboundMessageAsync(SnsMessage("OrderPlaced", "{}")));
    }

    [Test]
    public async Task AMessageNotRecognisedByAnEarlierDiscriminatorFallsThroughToTheSubject()
    {
        var options = SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions;
        var resolver = new DiscriminatingInboundMessageSerializerResolver(
            [new FixedTypeDiscriminator(null), new SubjectMessageTypeDiscriminator()],
            new Dictionary<string, IMessageBodySerializer>(StringComparer.Ordinal)
            {
                ["OrderPlaced"] = new SystemTextJsonMessageBodySerializer<OrderPlaced>(options).Erase(),
            });
        var converter = new InboundMessageConverter(resolver, new MessageCompressionRegistry(), isRawMessage: false);

        var placed = await converter.ConvertToInboundMessageAsync(SnsMessage("OrderPlaced", """{"OrderId":"order-1"}"""));

        placed.Message.ShouldBeOfType<OrderPlaced>().OrderId.ShouldBe("order-1");
    }

    private sealed class FixedTypeDiscriminator(string typeName) : IMessageTypeDiscriminator
    {
        public bool TryGetMessageTypeName(MessageDiscriminationContext context, out string result)
        {
            result = typeName;
            return typeName is not null;
        }
    }

    [Test]
    public void SubjectDiscriminatorReadsTheSubject()
    {
        var discriminator = new SubjectMessageTypeDiscriminator();

        discriminator.TryGetMessageTypeName(new MessageDiscriminationContext("{}", "OrderPlaced", new()), out var withSubject).ShouldBeTrue();
        withSubject.ShouldBe("OrderPlaced");

        discriminator.TryGetMessageTypeName(new MessageDiscriminationContext("{}", null, new()), out _).ShouldBeFalse();
    }
}
