using System.Text.Json;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.CloudEvents.Tests;

public class WhenSerializingCloudEvents
{
    private sealed class OrderPlaced : Message
    {
        public string OrderId { get; set; }
    }

    private sealed class PocoOrder
    {
        public string OrderId { get; set; }
    }

    private static CloudEventMessageBodySerializer<T> CreateSerializer<T>(string type) where T : class
    {
        var data = new SystemTextJsonMessageBodySerializer<T>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);
        var metadata = new MessagingConfig().MessageMetadataProvider;
        return new CloudEventMessageBodySerializer<T>(data, metadata, new Uri("https://orders.justeattakeaway.com/"), type);
    }

    [Test]
    public async Task Writes_A_Spec_Compliant_Structured_Envelope()
    {
        var serializer = CreateSerializer<OrderPlaced>("com.justeattakeaway.orders.orderplaced");
        var message = new OrderPlaced { OrderId = "order-123" };

        using var doc = JsonDocument.Parse(serializer.Serialize(message));
        var root = doc.RootElement;

        await Assert.That(root.GetProperty("specversion").GetString()).IsEqualTo("1.0");
        await Assert.That(root.GetProperty("type").GetString()).IsEqualTo("com.justeattakeaway.orders.orderplaced");
        await Assert.That(root.GetProperty("source").GetString()).IsEqualTo("https://orders.justeattakeaway.com/");
        await Assert.That(root.GetProperty("id").GetString()).IsEqualTo(message.Id.ToString());
        await Assert.That(root.GetProperty("datacontenttype").GetString()).IsEqualTo("application/json");
        await Assert.That(root.GetProperty("data").GetProperty("OrderId").GetString()).IsEqualTo("order-123");
    }

    [Test]
    public async Task Round_Trips_A_Message()
    {
        var serializer = CreateSerializer<OrderPlaced>("com.justeattakeaway.orders.orderplaced");
        var message = new OrderPlaced { OrderId = "order-123" };

        var result = serializer.Deserialize(serializer.Serialize(message));

        await Assert.That(result.OrderId).IsEqualTo("order-123");
        await Assert.That(result.Id).IsEqualTo(message.Id);
    }

    [Test]
    public async Task Generates_An_Id_For_A_Non_Message_Payload()
    {
        var serializer = CreateSerializer<PocoOrder>("com.justeattakeaway.orders.pocoorder");

        using var doc = JsonDocument.Parse(serializer.Serialize(new PocoOrder { OrderId = "poco-1" }));

        await Assert.That(Guid.TryParse(doc.RootElement.GetProperty("id").GetString(), out _)).IsTrue();
        await Assert.That(doc.RootElement.GetProperty("data").GetProperty("OrderId").GetString()).IsEqualTo("poco-1");
    }

    [Test]
    public async Task Factory_Throws_When_No_Type_Is_Configured()
    {
        var factory = new CloudEventSerializationFactory(
            new SystemTextJsonSerializationFactory(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions),
            new MessagingConfig().MessageMetadataProvider,
            new CloudEventOptions { Source = new Uri("https://example.com/") });

        await Assert.That(() => { factory.GetSerializer<OrderPlaced>(); }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Factory_Produces_A_Serializer_For_A_Mapped_Type()
    {
        var options = new CloudEventOptions { Source = new Uri("https://example.com/") }
            .WithCloudEventType<OrderPlaced>("com.justeattakeaway.orders.orderplaced");

        var factory = new CloudEventSerializationFactory(
            new SystemTextJsonSerializationFactory(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions),
            new MessagingConfig().MessageMetadataProvider,
            options);

        using var doc = JsonDocument.Parse(factory.GetSerializer<OrderPlaced>().Serialize(new OrderPlaced { OrderId = "x" }));

        await Assert.That(doc.RootElement.GetProperty("type").GetString()).IsEqualTo("com.justeattakeaway.orders.orderplaced");
    }
}
