using JustSaying.Messaging.Metadata;

namespace JustSaying.AsyncApi.Tests;

public class MessagingMetadataRegistryTests
{
    private sealed class OrderPlaced;

    [Test]
    public async Task DuplicatePublicationsAreRecordedOnce()
    {
        var registry = new MessagingMetadataRegistry();
        var messages = new[] { new MessageTypeMetadata(typeof(OrderPlaced), "orderplaced") };

        // Publication configuration runs for both the publisher and the batch publisher.
        registry.AddPublication(new PublicationMetadata(MessagingDestinationKind.SnsTopic, "orderplaced", false, messages));
        registry.AddPublication(new PublicationMetadata(MessagingDestinationKind.SnsTopic, "orderplaced", false, messages));

        await Assert.That(registry.Publications.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DuplicateSubscriptionsAreRecordedOnce()
    {
        var registry = new MessagingMetadataRegistry();
        var messages = new[] { new MessageTypeMetadata(typeof(OrderPlaced), "orderplaced") };

        registry.AddSubscription(new SubscriptionMetadata("orders", "orderplaced", "orders", false, messages));
        registry.AddSubscription(new SubscriptionMetadata("orders", "orderplaced", "orders", false, messages));

        await Assert.That(registry.Subscriptions.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TheFirstRegionWins()
    {
        var registry = new MessagingMetadataRegistry();
        registry.SetRegion("eu-west-1");
        registry.SetRegion("us-east-1");

        await Assert.That(registry.Region).IsEqualTo("eu-west-1");
    }
}
