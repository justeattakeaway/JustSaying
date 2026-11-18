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
    public async Task DoesNotClaimANativePayloadThatHappensToHaveATypeMember()
    {
        // A domain model with its own `type` property is not a CloudEvent, however much its value
        // looks like one — claiming it would make a mixed queue's routing registration-order dependent.
        const string body = """{"OrderId":"1","type":"com.justeattakeaway.orders.orderplaced"}""";

        var resolved = new CloudEventTypeDiscriminator()
            .TryGetMessageTypeName(new MessageDiscriminationContext(body, null, new()), out var typeName);

        await Assert.That(resolved).IsFalse();
        await Assert.That(typeName).IsNull();
    }

    [Test]
    [Arguments("""{"id":"1","source":"https://orders.example.com/","type":"orderplaced"}""")]
    [Arguments("""{"specversion":"1.0","source":"https://orders.example.com/","type":"orderplaced"}""")]
    [Arguments("""{"specversion":"1.0","id":"1","type":"orderplaced"}""")]
    [Arguments("""{"specversion":"1.0","id":"1","source":"https://orders.example.com/"}""")]
    [Arguments("""{"specversion":"1.0","id":"1","source":"https://orders.example.com/","type":""}""")]
    [Arguments("""{"specversion":1.0,"id":"1","source":"https://orders.example.com/","type":"orderplaced"}""")]
    public async Task ReturnsFalseWhenARequiredCloudEventAttributeIsMissing(string body)
    {
        var resolved = new CloudEventTypeDiscriminator()
            .TryGetMessageTypeName(new MessageDiscriminationContext(body, null, new()), out _);

        await Assert.That(resolved).IsFalse();
    }
}
