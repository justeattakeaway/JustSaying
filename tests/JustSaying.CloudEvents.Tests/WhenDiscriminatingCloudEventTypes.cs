using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.CloudEvents.Tests;

public class WhenDiscriminatingCloudEventTypes
{
    private sealed class OrderPlaced : Message
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task ReadsTheTypeFromAStructuredCloudEvent()
    {
        var inner = new SystemTextJsonMessageBodySerializer<OrderPlaced>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);
        var metadata = new MessagingConfig().MessageMetadataProvider;
        var serializer = new CloudEventMessageBodySerializer<OrderPlaced>(inner, metadata, new Uri("https://orders.example.com/"), "com.justeattakeaway.orders.orderplaced");

        var cloudEventJson = serializer.Serialize(new OrderPlaced { OrderId = "1" });

        var resolved = new CloudEventTypeDiscriminator()
            .TryGetMessageTypeName(new MessageDiscriminationContext(cloudEventJson, null, new()), out var typeName);

        await Assert.That(resolved).IsTrue();
        await Assert.That(typeName).IsEqualTo("com.justeattakeaway.orders.orderplaced");
    }

    [Test]
    public async Task ReturnsFalseForANonCloudEventBody()
    {
        var discriminator = new CloudEventTypeDiscriminator();

        await Assert.That(discriminator.TryGetMessageTypeName(new MessageDiscriminationContext("{\"foo\":1}", null, new()), out _)).IsFalse();
        await Assert.That(discriminator.TryGetMessageTypeName(new MessageDiscriminationContext("not json", null, new()), out _)).IsFalse();
    }
}
