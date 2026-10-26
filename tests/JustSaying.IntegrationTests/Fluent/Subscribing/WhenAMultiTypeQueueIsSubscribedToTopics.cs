using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

/// <summary>
/// A multi-type queue can be subscribed to SNS topics, so the types published to several topics can
/// share one queue (the v8 <c>ForTopic&lt;A&gt;()</c> + <c>ForTopic&lt;B&gt;()</c> shape) without two
/// subscriptions competing for its messages.
/// </summary>
public class WhenAMultiTypeQueueIsSubscribedToTopics : IntegrationTestBase
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class ParcelShipped
    {
        public string TrackingId { get; set; }
    }

    [Test]
    public async Task Then_Each_Type_Published_To_Its_Own_Topic_Is_Handled()
    {
        // Arrange
        var placedHandled = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);
        var shippedHandled = new TaskCompletionSource<ParcelShipped>(TaskCreationOptions.RunContinuationsAsynchronously);

        var placedHandler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        placedHandler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => placedHandled.TrySetResult(call.Arg<OrderPlaced>()));

        var shippedHandler = Substitute.For<IHandlerAsync<ParcelShipped>>();
        shippedHandler.Handle(Arg.Any<ParcelShipped>())
            .Returns(true)
            .AndDoes(call => shippedHandled.TrySetResult(call.Arg<ParcelShipped>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                // Each type is published to its own topic, one named by convention and one explicitly.
                .Publications(p =>
                {
                    p.WithTopic<OrderPlaced>();
                    p.WithTopic<ParcelShipped>(TopicDestination.Named($"{UniqueName}-parcels"));
                })
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .Handling<OrderPlaced>()
                    .Handling<ParcelShipped>()
                    .SubscribeToTopic<OrderPlaced>()
                    .SubscribeToTopic(TopicDestination.Named($"{UniqueName}-parcels")))))
            .AddSingleton(placedHandler)
            .AddSingleton(shippedHandler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-1" }, cancellationToken);
                await publisher.PublishAsync(new ParcelShipped { TrackingId = "parcel-1" }, cancellationToken);

                // Assert
                (await placedHandled.Task.WaitAsync(cancellationToken)).OrderId.ShouldBe("order-1");
                (await shippedHandled.Task.WaitAsync(cancellationToken)).TrackingId.ShouldBe("parcel-1");
            });
    }

    [Test]
    public async Task Then_The_Filter_Policy_Of_Each_Topic_Subscription_Is_Honoured()
    {
        // Arrange
        var handled = new List<OrderPlaced>();
        var euHandled = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);

        var placedHandler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        placedHandler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call =>
            {
                lock (handled)
                {
                    handled.Add(call.Arg<OrderPlaced>());
                }

                if (call.Arg<OrderPlaced>().OrderId == "eu-order")
                {
                    euHandled.TrySetResult(call.Arg<OrderPlaced>());
                }
            });

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithTopic<OrderPlaced>())
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .Handling<OrderPlaced>()
                    .Handling<ParcelShipped>()
                    .SubscribeToTopic<OrderPlaced>("""{ "region": ["eu"] }"""))))
            .AddSingleton(placedHandler)
            .AddSingleton(Substitute.For<IHandlerAsync<ParcelShipped>>());

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var us = new PublishMetadata();
                us.AddMessageAttribute("region", "us");
                var eu = new PublishMetadata();
                eu.AddMessageAttribute("region", "eu");

                // Act - the filtered-out message is published first, so it would arrive first if delivered.
                await publisher.PublishAsync(new OrderPlaced { OrderId = "us-order" }, us, cancellationToken);
                await publisher.PublishAsync(new OrderPlaced { OrderId = "eu-order" }, eu, cancellationToken);

                // Assert
                await euHandled.Task.WaitAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

                lock (handled)
                {
                    handled.ShouldHaveSingleItem().OrderId.ShouldBe("eu-order");
                }
            });
    }

    [Test]
    public async Task Then_The_Queue_Is_Subscribed_To_Each_Topic_With_Raw_Delivery_When_Configured()
    {
        // Arrange - raw delivery strips the Subject, so the types are routed by a body discriminator.
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .WithDiscriminator(new KindDiscriminator())
                    .Handling<OrderPlaced>("placed")
                    .Handling<ParcelShipped>("shipped")
                    .WithRawMessageDelivery()
                    .SubscribeToTopic<OrderPlaced>()
                    .SubscribeToTopic<ParcelShipped>())))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>())
            .AddSingleton(Substitute.For<IHandlerAsync<ParcelShipped>>());

        await WhenAsync(
            services,
            async (_, listener, cancellationToken) =>
            {
                // Act
                await listener.StartAsync(cancellationToken);

                // Assert
                var client = CreateClientFactory().GetSnsClient(Region);
                var topics = await client.ListTopicsAsync(cancellationToken);
                topics.Topics.Count.ShouldBe(2);

                foreach (var topic in topics.Topics)
                {
                    var subscriptions = await client.ListSubscriptionsByTopicAsync(topic.TopicArn, cancellationToken);
                    var subscription = subscriptions.Subscriptions.ShouldHaveSingleItem();
                    subscription.Endpoint.ShouldEndWith($":{UniqueName}");

                    var attributes = await client.GetSubscriptionAttributesAsync(subscription.SubscriptionArn, cancellationToken);
                    bool.Parse(attributes.Attributes["RawMessageDelivery"]).ShouldBeTrue();
                }
            });
    }

    [Test]
    public void Then_Raw_Delivery_Routed_By_Subject_Fails()
    {
        // Arrange - raw delivery strips the SNS Subject the types would be routed by.
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .Handling<OrderPlaced>()
                    .Handling<ParcelShipped>()
                    .WithRawMessageDelivery()
                    .SubscribeToTopic<OrderPlaced>()
                    .SubscribeToTopic<ParcelShipped>())))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>())
            .AddSingleton(Substitute.For<IHandlerAsync<ParcelShipped>>())
            .BuildServiceProvider();

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagingBus>())
            .Message.ShouldContain("uses raw message delivery but routes messages by the SNS Subject");
    }

    [Test]
    public void Then_A_Queue_Addressed_By_Url_Cannot_Be_Subscribed_To_Topics()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(
                    QueueDestination.FromUrl($"https://sqs.{RegionName}.amazonaws.com/123456789012/{UniqueName}"),
                    q => q.Handling<OrderPlaced>().SubscribeToTopic<OrderPlaced>())))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>())
            .BuildServiceProvider();

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagingBus>())
            .Message.ShouldContain("cannot be addressed by URL or ARN");
    }

    // Only needs to exist: this test checks the infrastructure, not routing.
    private sealed class KindDiscriminator : IMessageTypeDiscriminator
    {
        public bool TryGetMessageTypeName(MessageDiscriminationContext context, out string typeName)
        {
            typeName = null;
            return false;
        }
    }
}
