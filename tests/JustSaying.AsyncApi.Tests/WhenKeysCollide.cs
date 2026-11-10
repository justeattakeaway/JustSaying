using ByteBard.AsyncAPI.Models;
using JustSaying.Messaging.Metadata;

namespace JustSaying.AsyncApi.Tests;

public class WhenKeysCollide
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderReady
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task WireNamesThatSanitizeToTheSameKeyBecomeDistinctMessages()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");

        // Neither wire name is a valid AsyncAPI key, and both sanitize to "orders_placed";
        // one must not silently overwrite the other.
        registry.AddPublication(new PublicationMetadata(
            MessagingDestinationKind.SnsTopic,
            "orders",
            false,
            [
                new MessageTypeMetadata(typeof(OrderPlaced), "orders/placed"),
                new MessageTypeMetadata(typeof(OrderReady), "orders_placed"),
            ]));

        var document = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions()).Generate();

        var channel = document.Channels["orders"];
        await Assert.That(channel.Messages.Count).IsEqualTo(2);
        await Assert.That(channel.Messages["orders_placed"].Name).IsEqualTo("orders/placed");
        await Assert.That(channel.Messages["orders_placed-2"].Name).IsEqualTo("orders_placed");

        // Both messages are referenced, and each reference resolves to the message it names.
        var references = document.Operations["send-orders"].Messages
            .Select((m) => ((AsyncApiMessageReference)m).Reference.Reference)
            .ToList();

        await Assert.That(references).Contains("#/channels/orders/messages/orders_placed");
        await Assert.That(references).Contains("#/channels/orders/messages/orders_placed-2");
    }

    [Test]
    public async Task ADestinationNamedLikeTheFallbackKeyDoesNotStealTheChannel()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");

        MessageTypeMetadata[] placed = [new(typeof(OrderPlaced), "orderplaced")];
        MessageTypeMetadata[] ready = [new(typeof(OrderReady), "orderready")];

        // Four different destinations, three of which want the key "orders": the topic takes
        // it, the queue of the same name falls back to "orders-queue", and the cross-region
        // queue's last fallback is already occupied by a queue genuinely named that.
        registry.AddPublication(new PublicationMetadata(MessagingDestinationKind.SnsTopic, "orders", false, placed));
        registry.AddSubscription(new SubscriptionMetadata("orders", null, "orders", false, placed));
        registry.AddSubscription(new SubscriptionMetadata("orders-queue-us-east-1", null, "orders", false, placed));
        registry.AddSubscription(new SubscriptionMetadata("orders", null, "orders", false, ready, "us-east-1"));

        var document = new AsyncApiDocumentGenerator(registry, new AsyncApiOptions()).Generate();

        await Assert.That(document.Channels.Count).IsEqualTo(4);
        await Assert.That(document.Channels["orders"].Address).IsEqualTo("orders");
        await Assert.That(document.Channels["orders-queue"].Address).IsEqualTo("orders");
        await Assert.That(document.Channels["orders-queue-us-east-1"].Address).IsEqualTo("orders-queue-us-east-1");

        // The cross-region queue gets a channel of its own rather than mixing its messages
        // into the queue that already owns the fallback key.
        var crossRegion = document.Channels["orders-queue-us-east-1-2"];
        await Assert.That(crossRegion.Address).IsEqualTo("orders");
        await Assert.That(crossRegion.Messages.Keys).Contains("orderready");
        await Assert.That(((AsyncApiServerReference)crossRegion.Servers[0]).Reference.Reference).IsEqualTo("#/servers/sqs-us-east-1");

        await Assert.That(document.Channels["orders-queue-us-east-1"].Messages.Keys).Contains("orderplaced");
        await Assert.That(document.Channels["orders-queue-us-east-1"].Messages.Count).IsEqualTo(1);
    }
}
