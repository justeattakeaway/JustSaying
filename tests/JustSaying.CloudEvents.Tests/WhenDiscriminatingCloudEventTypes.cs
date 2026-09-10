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

    [Test]
    public async Task ReturnsFalseForANonCloudEventThatHappensToHaveAType()
    {
        var discriminator = new CloudEventTypeDiscriminator();

        // A bare "type" property is far too weak a signal to claim a payload as a CloudEvent.
        var body = """{"type":"pepperoni","size":"large"}""";

        await Assert.That(discriminator.TryGetMessageTypeName(new MessageDiscriminationContext(body, null, new()), out var typeName)).IsFalse();
        await Assert.That(typeName).IsNull();
    }

    [Test]
    public async Task ReturnsFalseWhenSpecVersionIsMissingOrEmpty()
    {
        var discriminator = new CloudEventTypeDiscriminator();

        var noSpecVersion = """{"id":"1","source":"https://example.com/","type":"com.example.thing"}""";
        var emptySpecVersion = """{"specversion":"","id":"1","source":"https://example.com/","type":"com.example.thing"}""";

        await Assert.That(discriminator.TryGetMessageTypeName(new MessageDiscriminationContext(noSpecVersion, null, new()), out _)).IsFalse();
        await Assert.That(discriminator.TryGetMessageTypeName(new MessageDiscriminationContext(emptySpecVersion, null, new()), out _)).IsFalse();
    }
}
