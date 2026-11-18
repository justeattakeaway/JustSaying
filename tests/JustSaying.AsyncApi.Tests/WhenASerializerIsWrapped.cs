using System.Text.Json;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Metadata;

namespace JustSaying.AsyncApi.Tests;

/// <summary>
/// A message's format is described by the serializer its registration uses, through
/// <see cref="IMessageBodyFormat"/>, so a serializer that wraps another can forward the description
/// and keep the payload schema.
/// </summary>
public class WhenASerializerIsWrapped
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private sealed class LoggingSerializer<T>(SystemTextJsonMessageBodySerializer<T> inner) : IMessageBodySerializer<T>, ISystemTextJsonMessageBodySerializer
        where T : class
    {
        public string ContentType => inner.ContentType;

        public JsonSerializerOptions SerializerOptions => inner.SerializerOptions;

        public string Serialize(T message) => inner.Serialize(message);

        public T Deserialize(string message) => inner.Deserialize(message);
    }

    [Test]
    public async Task ForwardingTheFormatKeepsThePayloadSchema()
    {
        var serializer = new LoggingSerializer<OrderPlaced>(new SystemTextJsonMessageBodySerializer<OrderPlaced>(new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "order-placed",
            isDynamic: false,
            [new MessageTypeMetadata(typeof(OrderPlaced), nameof(OrderPlaced), serializer)]));

        var document = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions()).Generate();

        // The wrapped serializer's (camel-case) options shape the schema.
        var message = document.Channels["order-placed"].Messages[nameof(OrderPlaced)];
        await Assert.That(message.ContentType).IsEqualTo("application/json");
        await Assert.That(((ByteBard.AsyncAPI.Models.AsyncApiJsonSchema)message.Payload.Schema).Properties.Keys).Contains("orderId");
    }
}
