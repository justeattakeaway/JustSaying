using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.UnitTests.Messaging.MessageSerialization;

/// <summary>
/// The two built-in serializers disagree about what a derived instance passed as its base type puts on
/// the wire: System.Text.Json serializes the declared type, Newtonsoft.Json serializes the runtime type.
/// JustSaying's own publish path uses the publication — and therefore the serializer — registered for
/// the concrete runtime type when there is one, so the two only differ for a message published to a
/// publication registered for its base type; these tests pin that difference.
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
