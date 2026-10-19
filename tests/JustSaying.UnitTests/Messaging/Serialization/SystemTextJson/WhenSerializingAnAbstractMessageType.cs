using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson;

/// <summary>
/// System.Text.Json serializes a message as the serializer's declared type, so a serializer for an
/// abstract class or interface only writes the members of derived types (and the discriminator a
/// consumer needs) when the type is configured for polymorphism. Without it, creating the serializer
/// fails rather than silently dropping data.
/// </summary>
public class WhenSerializingAnAbstractMessageType
{
    public abstract class PlainWarehouseEvent
    {
        public string Sku { get; set; }
    }

    public interface IPlainWarehouseEvent
    {
        string Sku { get; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(StockReserved), "reserved")]
    public abstract class WarehouseEvent
    {
        public string Sku { get; set; }
    }

    public sealed class StockReserved : WarehouseEvent
    {
        public int Quantity { get; set; }
    }

    [JsonDerivedType(typeof(StockCounted), "counted")]
    public interface IWarehouseEvent
    {
        string Sku { get; }
    }

    public sealed class StockCounted : IWarehouseEvent
    {
        public string Sku { get; set; }

        public int Count { get; set; }
    }

    public sealed class StockReleased : PlainWarehouseEvent
    {
        public string Reason { get; set; }
    }

    [Test]
    public void AnAbstractClassWithoutPolymorphismThrows()
    {
        var exception = Should.Throw<InvalidOperationException>(
            () => new SystemTextJsonMessageBodySerializer<PlainWarehouseEvent>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions));

        exception.Message.ShouldContain($"'{typeof(PlainWarehouseEvent)}' is abstract");
        exception.Message.ShouldContain("silently dropped");
        exception.Message.ShouldContain("[JsonPolymorphic]");
    }

    [Test]
    public void AnInterfaceWithoutPolymorphismThrows()
    {
        var exception = Should.Throw<InvalidOperationException>(
            () => new SystemTextJsonMessageBodySerializer<IPlainWarehouseEvent>(new JsonSerializerOptions()));

        exception.Message.ShouldContain($"'{typeof(IPlainWarehouseEvent)}' is an interface");
    }

    [Test]
    public void ASourceGeneratedContextWithoutPolymorphismThrows()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = AbstractMessageTypeContext.Default };

        Should.Throw<InvalidOperationException>(() => new SystemTextJsonMessageBodySerializer<PlainWarehouseEvent>(options));
    }

    [Test]
    public void APolymorphicAbstractClassRoundTripsDerivedMessages()
    {
        var serializer = new SystemTextJsonMessageBodySerializer<WarehouseEvent>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

        var json = serializer.Serialize(new StockReserved { Sku = "sku-1", Quantity = 2 });

        json.ShouldContain("\"kind\":\"reserved\"");
        json.ShouldContain("\"Quantity\":2");
        serializer.Deserialize(json).ShouldBeOfType<StockReserved>().Quantity.ShouldBe(2);
    }

    [Test]
    public void APolymorphicInterfaceRoundTripsDerivedMessages()
    {
        var serializer = new SystemTextJsonMessageBodySerializer<IWarehouseEvent>(new JsonSerializerOptions());

        var json = serializer.Serialize(new StockCounted { Sku = "sku-1", Count = 5 });

        json.ShouldContain("\"Count\":5");
        serializer.Deserialize(json).ShouldBeOfType<StockCounted>().Count.ShouldBe(5);
    }

    [Test]
    public void APolymorphicTypeInASourceGeneratedContextIsAccepted()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = AbstractMessageTypeContext.Default };
        var serializer = new SystemTextJsonMessageBodySerializer<WarehouseEvent>(options);

        serializer.Serialize(new StockReserved { Sku = "sku-1", Quantity = 2 }).ShouldContain("\"kind\":\"reserved\"");
    }

    [Test]
    public void PolymorphismConfiguredByAResolverIsAccepted()
    {
        var resolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                typeInfo =>
                {
                    if (typeInfo.Type == typeof(PlainWarehouseEvent))
                    {
                        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                        {
                            DerivedTypes = { new JsonDerivedType(typeof(StockReleased), "released") },
                        };
                    }
                },
            },
        };

        var serializer = new SystemTextJsonMessageBodySerializer<PlainWarehouseEvent>(new JsonSerializerOptions { TypeInfoResolver = resolver });

        serializer.Serialize(new StockReleased { Sku = "sku-1", Reason = "cancelled" }).ShouldContain("\"Reason\":\"cancelled\"");
    }

    [Test]
    public void AnAbstractClassWithACustomConverterIsAccepted()
    {
        var options = new JsonSerializerOptions { Converters = { new PlainWarehouseEventConverter() } };

        var serializer = new SystemTextJsonMessageBodySerializer<PlainWarehouseEvent>(options);

        serializer.Serialize(new StockReleased { Sku = "sku-1", Reason = "cancelled" }).ShouldContain("\"Reason\":\"cancelled\"");
    }

    private sealed class PlainWarehouseEventConverter : JsonConverter<PlainWarehouseEvent>
    {
        public override PlainWarehouseEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<StockReleased>(ref reader);

        public override void Write(Utf8JsonWriter writer, PlainWarehouseEvent value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, (StockReleased)value);
    }
}

[JsonSerializable(typeof(WhenSerializingAnAbstractMessageType.PlainWarehouseEvent))]
[JsonSerializable(typeof(WhenSerializingAnAbstractMessageType.WarehouseEvent))]
internal sealed partial class AbstractMessageTypeContext : JsonSerializerContext;
