using System.Text;
using System.Text.Json;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents.Tests;

public class WhenValidatingCloudEvents
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    private sealed class PocoOrder
    {
        public string OrderId { get; set; }
    }

    private sealed class EmptyIdMetadataProvider : IMessageMetadataProvider
    {
        public string GetId(object message) => "";

        public DateTimeOffset? GetTimestamp(object message) => null;

        public bool TryGetDeduplicationKey(object message, out string deduplicationKey)
        {
            deduplicationKey = null;
            return false;
        }
    }

    private static CloudEventMessageBodySerializer<PocoOrder> CreateSerializer(
        Uri source = null,
        IMessageMetadataProvider metadataProvider = null,
        string dataContentType = "application/json")
        => new(
            new SystemTextJsonMessageBodySerializer<PocoOrder>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions),
            metadataProvider ?? new MessagingConfig().MessageMetadataProvider,
            source ?? new Uri("https://orders.example.com"),
            OrderPlacedType,
            dataContentType);

    private static string Event(string members)
        => $$"""{"specversion":"1.0","id":"evt-1","source":"/orders","type":"{{OrderPlacedType}}"{{members}}}""";

    [Test]
    [Arguments("https://Orders.Example.com")]
    [Arguments("urn:example:orders%20eu")]
    [Arguments("/orders")]
    public async Task Writes_The_Source_As_Configured(string source)
    {
        var serializer = CreateSerializer(new Uri(source, UriKind.RelativeOrAbsolute));

        using var doc = JsonDocument.Parse(serializer.Serialize(new PocoOrder { OrderId = "1" }));

        await Assert.That(doc.RootElement.GetProperty("source").GetString()).IsEqualTo(source);
    }

    [Test]
    public async Task Mints_An_Id_When_The_Payload_Id_Is_Empty()
    {
        var serializer = CreateSerializer(metadataProvider: new EmptyIdMetadataProvider());

        using var doc = JsonDocument.Parse(serializer.Serialize(new PocoOrder { OrderId = "1" }));

        await Assert.That(Guid.TryParse(doc.RootElement.GetProperty("id").GetString(), out _)).IsTrue();
    }

    [Test]
    public async Task Rejects_An_Empty_Source()
    {
        await Assert.That(() => { CreateSerializer(new Uri("", UriKind.Relative)); }).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("text/plain")]
    [Arguments("application/xml")]
    public async Task Rejects_A_Non_Json_Data_Content_Type(string dataContentType)
    {
        await Assert.That(() => { CreateSerializer(dataContentType: dataContentType); }).Throws<ArgumentException>();
        await Assert.That(() => { new CloudEventOptions().DataContentType = dataContentType; }).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("application/json")]
    [Arguments("application/json; charset=utf-8")]
    [Arguments("application/vnd.example.order+json")]
    [Arguments("text/json")]
    public async Task Accepts_A_Json_Data_Content_Type(string dataContentType)
    {
        var options = new CloudEventOptions { DataContentType = dataContentType };
        var serializer = CreateSerializer(dataContentType: options.DataContentType);

        using var doc = JsonDocument.Parse(serializer.Serialize(new PocoOrder { OrderId = "1" }));

        await Assert.That(doc.RootElement.GetProperty("datacontenttype").GetString()).IsEqualTo(dataContentType);
    }

    [Test]
    public async Task Rejects_An_Event_Of_Another_Type()
    {
        var body = """{"specversion":"1.0","id":"evt-1","source":"/orders","type":"com.example.refund.issued","data":{"OrderId":"1"}}""";

        await Assert.That(() => { CreateSerializer().Deserialize(body); })
            .Throws<InvalidOperationException>()
            .WithMessageContaining("com.example.refund.issued");
    }

    [Test]
    [Arguments("\"0.3\"")]
    [Arguments("\"2.0\"")]
    [Arguments("1.0")]
    public async Task Rejects_An_Unsupported_Specversion(string specVersion)
    {
        var body = $$$"""{"specversion":{{{specVersion}}},"id":"evt-1","source":"/orders","type":"{{{OrderPlacedType}}}","data":{"OrderId":"1"}}""";

        await Assert.That(() => { CreateSerializer().Deserialize(body); }).Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(",\"data\":null")]
    [Arguments("")]
    public async Task Rejects_An_Event_With_No_Data(string members)
    {
        await Assert.That(() => { CreateSerializer().Deserialize(Event(members)); })
            .Throws<InvalidOperationException>()
            .WithMessageContaining("no data");
    }

    [Test]
    [Arguments(",\"datacontenttype\":\"application/json\"")]
    [Arguments("")]
    public async Task Reads_Json_Data_Base64(string contentType)
    {
        var dataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"OrderId":"order-64"}"""));

        var result = CreateSerializer().Deserialize(Event($$"""{{contentType}},"data_base64":"{{dataBase64}}" """));

        await Assert.That(result.OrderId).IsEqualTo("order-64");
    }

    [Test]
    public async Task Rejects_Non_Json_Data_Base64()
    {
        var dataBase64 = Convert.ToBase64String([0x01, 0x02]);

        await Assert.That(() => { CreateSerializer().Deserialize(Event($$""","datacontenttype":"application/octet-stream","data_base64":"{{dataBase64}}" """)); })
            .Throws<InvalidOperationException>()
            .WithMessageContaining("application/octet-stream");
    }

    [Test]
    public async Task Rejects_An_Event_With_Both_Data_And_Data_Base64()
    {
        var dataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"OrderId":"1"}"""));

        await Assert.That(() => { CreateSerializer().Deserialize(Event($$""","data":{"OrderId":"1"},"data_base64":"{{dataBase64}}" """)); })
            .Throws<InvalidOperationException>()
            .WithMessageContaining("both");
    }
}
