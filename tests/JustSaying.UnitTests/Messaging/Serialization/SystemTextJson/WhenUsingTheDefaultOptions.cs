using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson;

/// <summary>
/// The default options keep binding close to the Newtonsoft.Json defaults of JustSaying v8, so that
/// ordinary message types don't silently lose data when moving to System.Text.Json.
/// </summary>
public class WhenUsingTheDefaultOptions
{
    private sealed class OrderPlaced : Message
    {
        public string Channel;

        public string Note { get; set; }

        public int Quantity { get; set; }

        public List<string> Lines { get; } = [];
    }

    private sealed record OrderShipped(string OrderId)
    {
        public string Carrier { get; set; }
    }

    private static SystemTextJsonMessageBodySerializer<T> CreateSerializer<T>() where T : class
        => new(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

    [Test]
    public void PropertyNamesAreReadCaseInsensitively()
    {
        var message = CreateSerializer<OrderPlaced>().Deserialize(
            """{"id":"5b0f0a1e-1b2c-4d5e-8f90-123456789abc","note":"from-node","quantity":7}""");

        message.Id.ShouldBe(Guid.Parse("5b0f0a1e-1b2c-4d5e-8f90-123456789abc"));
        message.Note.ShouldBe("from-node");
        message.Quantity.ShouldBe(7);
    }

    [Test]
    public void GetOnlyCollectionsArePopulated()
    {
        var message = CreateSerializer<OrderPlaced>().Deserialize("""{"Lines":["SKU-1","SKU-2"]}""");

        message.Lines.ShouldBe(["SKU-1", "SKU-2"]);
    }

    [Test]
    public void RecordsWithConstructorParametersStillDeserialize()
    {
        // System.Text.Json can't populate a type that binds through a parameterized constructor, so
        // Populate must fall back to the constructor for these rather than throwing.
        var message = CreateSerializer<OrderShipped>().Deserialize("""{"orderId":"O-1","Carrier":"DHL"}""");

        message.OrderId.ShouldBe("O-1");
        message.Carrier.ShouldBe("DHL");
    }

    [Test]
    public void PublicFieldsRoundTrip()
    {
        var serializer = CreateSerializer<OrderPlaced>();

        var json = serializer.Serialize(new OrderPlaced { Channel = "web" });

        json.ShouldContain("\"Channel\":\"web\"");
        serializer.Deserialize(json).Channel.ShouldBe("web");
    }

    [Test]
    public void NonAsciiAndHtmlSensitiveCharactersAreNotEscaped()
    {
        var json = CreateSerializer<OrderPlaced>().Serialize(new OrderPlaced { Note = "日本語 <b>&'café'</b>" });

        json.ShouldContain("\"Note\":\"日本語 <b>&'café'</b>\"");
    }
}
