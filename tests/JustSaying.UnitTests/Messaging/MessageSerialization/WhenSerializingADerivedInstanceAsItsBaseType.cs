using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.UnitTests.Messaging.MessageSerialization;

/// <summary>
/// The two built-in serializers disagree about what a derived instance passed as its base type puts on
/// the wire: System.Text.Json serializes the declared type, Newtonsoft.Json serializes the runtime type.
/// JustSaying's own publish path resolves publishers — and therefore serializers — by the concrete
/// runtime type, so the two coincide there; these tests pin the difference for anyone constructing a
/// serializer for a base type directly.
/// </summary>
public class WhenSerializingADerivedInstanceAsItsBaseType
{
    private class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private sealed class OrderPlacedWithExtras : OrderPlaced
    {
        public string InternalNote { get; set; }
    }

    [Test]
    public void SystemTextJsonSerializesTheDeclaredTypeOnly()
    {
        var serializer = new SystemTextJsonMessageBodySerializer<OrderPlaced>();

        var json = serializer.Serialize(new OrderPlacedWithExtras { OrderId = "abc-123", InternalNote = "note" });

        json.ShouldContain("abc-123");
        json.ShouldNotContain("InternalNote");
    }

    [Test]
    public void NewtonsoftSerializesTheRuntimeType()
    {
        var serializer = new NewtonsoftMessageBodySerializer<OrderPlaced>();

        var json = serializer.Serialize(new OrderPlacedWithExtras { OrderId = "abc-123", InternalNote = "note" });

        json.ShouldContain("abc-123");
        json.ShouldContain("InternalNote");
    }
}
