using System.Text;
using System.Text.Json;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.CloudEvents.Tests;

public class WhenSerializingACloudEventEnvelope
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    private sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private static IMessageBodySerializer<CloudEvent<OrderPlaced>> CreateSerializer(string type = OrderPlacedType)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessagingConfig>(new MessagingConfig());
        services.AddJustSayingCloudEvents(options => options.Source = new Uri("/orders", UriKind.Relative));

        return services.BuildServiceProvider()
            .GetRequiredService<CloudEventSerializationFactory>()
            .GetEnvelopeSerializer<OrderPlaced>(type);
    }

    private static string Event(string attributes, string data = "\"data\": { \"OrderId\": \"order-1\" }")
        => $$"""
            {
              "specversion": "1.0",
              "id": "event-1",
              "source": "/orders",
              "type": "{{OrderPlacedType}}",
              {{attributes}}{{(attributes.Length > 0 ? "," : string.Empty)}}
              {{data}}
            }
            """;

    [Test]
    [Arguments("TenantId")]
    [Arguments("tenant-id")]
    [Arguments("tenant_id")]
    [Arguments("")]
    public async Task An_Invalid_Extension_Name_Is_Rejected_On_Construction(string name)
    {
        await Assert.That(() => new CloudEvent<OrderPlaced>(
                new OrderPlaced(),
                extensions: new Dictionary<string, string> { [name] = "acme" }))
            .Throws<ArgumentException>();
    }

    [Test]
    [Arguments("id")]
    [Arguments("subject")]
    [Arguments("dataschema")]
    [Arguments("data")]
    public async Task A_Context_Attribute_Name_Is_Rejected_As_An_Extension(string name)
    {
        await Assert.That(() => new CloudEvent<OrderPlaced>(
                new OrderPlaced(),
                extensions: new Dictionary<string, string> { [name] = "value" }))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task An_Inbound_Invalid_Extension_Is_Kept_But_Rejected_On_Republish()
    {
        var serializer = CreateSerializer();

        var received = serializer.Deserialize(Event("\"TenantId\": \"acme\""));

        await Assert.That(received.Extensions["TenantId"]).IsEqualTo("acme");
        await Assert.That(() => serializer.Serialize(received)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Integer_And_Boolean_Extensions_Are_Kept_As_Their_Json_Text()
    {
        var received = CreateSerializer().Deserialize(Event("\"seqno\": 42, \"replay\": true, \"unset\": null, \"tenant\": \"acme\""));

        await Assert.That(received.Extensions["seqno"]).IsEqualTo("42");
        await Assert.That(received.Extensions["replay"]).IsEqualTo("true");
        await Assert.That(received.Extensions["tenant"]).IsEqualTo("acme");
        await Assert.That(received.Extensions.ContainsKey("unset")).IsFalse();
    }

    [Test]
    [Arguments("\"specversion\": \"0.3\", \"id\": \"e\", \"source\": \"/s\", \"type\": \"com.example.orders.order.placed\"")]
    [Arguments("\"specversion\": \"2.0\", \"id\": \"e\", \"source\": \"/s\", \"type\": \"com.example.orders.order.placed\"")]
    [Arguments("\"id\": \"e\", \"source\": \"/s\", \"type\": \"com.example.orders.order.placed\"")]
    [Arguments("\"specversion\": \"1.0\", \"source\": \"/s\", \"type\": \"com.example.orders.order.placed\"")]
    [Arguments("\"specversion\": \"1.0\", \"id\": \"\", \"source\": \"/s\", \"type\": \"com.example.orders.order.placed\"")]
    [Arguments("\"specversion\": \"1.0\", \"id\": \"e\", \"type\": \"com.example.orders.order.placed\"")]
    [Arguments("\"specversion\": \"1.0\", \"id\": \"e\", \"source\": \"/s\"")]
    public async Task An_Event_Missing_A_Required_Attribute_Or_Of_Another_Spec_Version_Fails(string attributes)
    {
        var body = $$"""{ {{attributes}}, "data": { "OrderId": "order-1" } }""";

        await Assert.That(() => CreateSerializer().Deserialize(body)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task An_Event_Of_Another_Type_Fails()
    {
        await Assert.That(() => CreateSerializer("com.example.orders.order.cancelled").Deserialize(Event(string.Empty)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments("\"data\": null")]
    [Arguments("\"datacontenttype\": \"application/json\"")]
    public async Task An_Event_Without_Data_Fails_Rather_Than_Reaching_The_Handler(string data)
    {
        await Assert.That(() => CreateSerializer().Deserialize(Event(string.Empty, data))).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Json_Data_Sent_As_Base64_Is_Read()
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{ "OrderId": "order-64" }"""));

        var received = CreateSerializer().Deserialize(
            Event("\"datacontenttype\": \"application/json\"", $"\"data_base64\": \"{base64}\""));

        await Assert.That(received.Data.OrderId).IsEqualTo("order-64");
    }

    [Test]
    public async Task Data_And_Base64_Data_Together_Fail()
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{ "OrderId": "order-64" }"""));

        await Assert.That(() => CreateSerializer().Deserialize(
                Event($"\"data_base64\": \"{base64}\"")))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Non_Json_Base64_Data_Fails()
    {
        await Assert.That(() => CreateSerializer().Deserialize(
                Event("\"datacontenttype\": \"image/png\"", "\"data_base64\": \"iVBORw0KGgo=\"")))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task The_Data_Content_Type_And_Schema_Survive_A_Read_And_Republish()
    {
        var serializer = CreateSerializer();

        var received = serializer.Deserialize(Event(
            "\"datacontenttype\": \"application/vnd.orders+json; charset=utf-8\", \"dataschema\": \"https://schemas.example.com/order-placed/v2\""));

        await Assert.That(received.DataContentType).IsEqualTo("application/vnd.orders+json; charset=utf-8");
        await Assert.That(received.DataSchema).IsEqualTo(new Uri("https://schemas.example.com/order-placed/v2"));

        using var republished = JsonDocument.Parse(serializer.Serialize(received));
        await Assert.That(republished.RootElement.GetProperty("datacontenttype").GetString())
            .IsEqualTo("application/vnd.orders+json; charset=utf-8");
        await Assert.That(republished.RootElement.GetProperty("dataschema").GetString())
            .IsEqualTo("https://schemas.example.com/order-placed/v2");
    }

    [Test]
    [Arguments("https://Orders.Example.com")]
    [Arguments("urn:example:orders%20eu")]
    [Arguments("/orders/eu")]
    public async Task The_Source_Is_Written_Exactly_As_Given(string source)
    {
        var serializer = CreateSerializer();

        using var document = JsonDocument.Parse(serializer.Serialize(
            new CloudEvent<OrderPlaced>(new OrderPlaced(), source: new Uri(source, UriKind.RelativeOrAbsolute))));

        await Assert.That(document.RootElement.GetProperty("source").GetString()).IsEqualTo(source);
    }

    [Test]
    [Arguments("2026-10-01T10:00:00Z", "2026-10-01T10:00:00.0000000+00:00")]
    [Arguments("2026-10-01t10:00:00.123456789z", "2026-10-01T10:00:00.1234567+00:00")]
    [Arguments("2026-10-01T10:00:00+01:00", "2026-10-01T10:00:00.0000000+01:00")]
    public async Task An_Rfc3339_Time_Is_Read_With_Its_Offset(string time, string expected)
    {
        var received = CreateSerializer().Deserialize(Event($"\"time\": \"{time}\""));

        await Assert.That(received.Time?.ToString("O", System.Globalization.CultureInfo.InvariantCulture)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("2026-10-01T10:00:00")]
    [Arguments("2026-10-01 10:00:00Z")]
    [Arguments("2026-13-01T10:00:00Z")]
    [Arguments("2016-12-31T23:59:60Z")] // a leap second, which DateTimeOffset can't hold (the CloudEvents SDK rejects it too)
    [Arguments("yesterday")]
    public async Task A_Time_That_Is_Not_Rfc3339_Fails_Rather_Than_Being_Read_In_The_Local_Zone(string time)
    {
        await Assert.That(() => CreateSerializer().Deserialize(Event($"\"time\": \"{time}\""))).Throws<InvalidOperationException>();
    }
}
