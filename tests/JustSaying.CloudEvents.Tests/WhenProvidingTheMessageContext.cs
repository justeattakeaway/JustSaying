using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.CloudEvents.Tests;

public class WhenProvidingTheMessageContext
{
    private sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private static CloudEventMessageBodySerializer<OrderPlaced> CreateSerializer()
    {
        var data = new SystemTextJsonMessageBodySerializer<OrderPlaced>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);
        var metadata = new MessagingConfig().MessageMetadataProvider;
        return new CloudEventMessageBodySerializer<OrderPlaced>(data, metadata, new Uri("https://orders.justeattakeaway.com/"), "com.justeattakeaway.orders.orderplaced");
    }

    private static CloudEventMessageContext CreateContext(MessageContextFactory factory)
        => (CloudEventMessageContext)factory(
            new SQSMessage { MessageId = "sqs-1" },
            new Uri("https://sqs.eu-west-1.amazonaws.com/123456789012/orders"),
            new MessageAttributes());

    [Test]
    public async Task Captures_The_Envelope_Attributes()
    {
        const string envelope =
            """
            {
              "specversion": "1.0",
              "id": "event-42",
              "source": "/demo/orders",
              "type": "com.justeattakeaway.orders.orderplaced",
              "time": "2026-07-12T09:30:00Z",
              "datacontenttype": "application/json",
              "subject": "order-123",
              "traceparent": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
              "sequence": 7,
              "data": { "OrderId": "order-123" }
            }
            """;

        var message = CreateSerializer().Deserialize(envelope, out var contextFactory);

        await Assert.That(message.OrderId).IsEqualTo("order-123");
        await Assert.That(contextFactory).IsNotNull();

        var context = CreateContext(contextFactory);

        await Assert.That(context.SpecVersion).IsEqualTo("1.0");
        await Assert.That(context.Id).IsEqualTo("event-42");
        await Assert.That(context.Source.ToString()).IsEqualTo("/demo/orders");
        await Assert.That(context.Type).IsEqualTo("com.justeattakeaway.orders.orderplaced");
        await Assert.That(context.Time).IsEqualTo(new DateTimeOffset(2026, 7, 12, 9, 30, 0, TimeSpan.Zero));
        await Assert.That(context.DataContentType).IsEqualTo("application/json");
        await Assert.That(context.Subject).IsEqualTo("order-123");
        await Assert.That(context.Extensions["traceparent"]).IsEqualTo("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
        await Assert.That(context.Extensions["sequence"]).IsEqualTo("7");
        await Assert.That(context.Extensions.ContainsKey("data")).IsFalse();
        await Assert.That(context.Message.MessageId).IsEqualTo("sqs-1");
    }

    [Test]
    public async Task Round_Trips_Its_Own_Envelope()
    {
        var serializer = CreateSerializer();

        var message = serializer.Deserialize(serializer.Serialize(new OrderPlaced { OrderId = "order-9" }), out var contextFactory);
        var context = CreateContext(contextFactory);

        await Assert.That(message.OrderId).IsEqualTo("order-9");
        await Assert.That(context.Source.ToString()).IsEqualTo("https://orders.justeattakeaway.com/");
        await Assert.That(context.Type).IsEqualTo("com.justeattakeaway.orders.orderplaced");
        await Assert.That(context.Extensions).IsEmpty();
    }

    [Test]
    public async Task Falls_Back_To_The_Default_Context_When_Required_Attributes_Are_Missing()
    {
        var message = CreateSerializer().Deserialize("""{ "data": { "OrderId": "order-1" } }""", out var contextFactory);

        await Assert.That(message.OrderId).IsEqualTo("order-1");
        await Assert.That(contextFactory).IsNull();
    }

    [Test]
    public async Task The_Context_Reader_Extension_Filters_By_Context_Type()
    {
        var accessor = new MessageContextAccessor();
        var reader = (IMessageContextReader)accessor;

        await Assert.That(reader.GetCloudEventContext()).IsNull();

        var sqsMessage = new SQSMessage();
        var queueUri = new Uri("https://sqs.eu-west-1.amazonaws.com/123456789012/orders");

        accessor.MessageContext = new MessageContext(sqsMessage, queueUri, new MessageAttributes());
        await Assert.That(reader.GetCloudEventContext()).IsNull();

        accessor.MessageContext = new CloudEventMessageContext(
            sqsMessage, queueUri, new MessageAttributes(),
            "1.0", "event-1", new Uri("/demo/orders", UriKind.RelativeOrAbsolute), "com.justeattakeaway.orders.orderplaced");
        await Assert.That(reader.GetCloudEventContext()).IsNotNull();
    }
}
