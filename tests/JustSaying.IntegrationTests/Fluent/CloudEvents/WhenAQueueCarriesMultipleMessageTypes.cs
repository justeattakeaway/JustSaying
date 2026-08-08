using Amazon.SQS.Model;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// End-to-end coverage for multi-type-per-queue subscriptions: a single queue can carry more than one
/// message type, with each inbound message resolved from a wire discriminator and dispatched to the
/// handler registered for its own type. Covers both the default SNS <c>Subject</c> discriminator and
/// the CloudEvents <c>type</c> discriminator.
/// </summary>
public class WhenAQueueCarriesMultipleMessageTypes : IntegrationTestBase
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class OrderCancelled
    {
        public string Reason { get; set; }
    }

    [Test]
    public async Task Then_Each_Type_Is_Dispatched_By_Subject()
    {
        // Arrange
        var placedHandled = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelledHandled = new TaskCompletionSource<OrderCancelled>(TaskCreationOptions.RunContinuationsAsynchronously);

        var placedHandler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        placedHandler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => placedHandled.TrySetResult(call.Arg<OrderPlaced>()));

        var cancelledHandler = Substitute.For<IHandlerAsync<OrderCancelled>>();
        cancelledHandler.Handle(Arg.Any<OrderCancelled>())
            .Returns(true)
            .AndDoes(call => cancelledHandled.TrySetResult(call.Arg<OrderCancelled>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                // Both publishers target the same queue, so the queue carries both types.
                .Publications(p =>
                {
                    p.WithQueue<OrderPlaced>(o => o.WithQueueName(UniqueName));
                    p.WithQueue<OrderCancelled>(o => o.WithQueueName(UniqueName));
                })
                // One subscription over that queue, handling both types. The default Subject
                // discriminator resolves the type from the SNS Subject JustSaying writes on publish.
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .Handling<OrderPlaced>()
                    .Handling<OrderCancelled>())))
            .AddSingleton(placedHandler)
            .AddSingleton(cancelledHandler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-1" }, cancellationToken);
                await publisher.PublishAsync(new OrderCancelled { Reason = "out-of-stock" }, cancellationToken);

                // Assert
                (await placedHandled.Task.WaitAsync(cancellationToken)).OrderId.ShouldBe("order-1");
                (await cancelledHandled.Task.WaitAsync(cancellationToken)).Reason.ShouldBe("out-of-stock");
            });
    }

    [Test]
    public async Task Then_Each_Type_Is_Dispatched_By_CloudEvent_Type()
    {
        // Arrange
        const string placedType = "com.example.orders.order.placed";
        const string cancelledType = "com.example.orders.order.cancelled";

        var placedHandled = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelledHandled = new TaskCompletionSource<OrderCancelled>(TaskCreationOptions.RunContinuationsAsynchronously);

        var placedHandler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        placedHandler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => placedHandled.TrySetResult(call.Arg<OrderPlaced>()));

        var cancelledHandler = Substitute.For<IHandlerAsync<OrderCancelled>>();
        cancelledHandler.Handle(Arg.Any<OrderCancelled>())
            .Returns(true)
            .AndDoes(call => cancelledHandled.TrySetResult(call.Arg<OrderCancelled>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p =>
                {
                    p.WithQueue<OrderPlaced>(o => o.WithQueueName(UniqueName));
                    p.WithQueue<OrderCancelled>(o => o.WithQueueName(UniqueName));
                })
                // Resolve each inbound message's type from the CloudEvents `type` attribute in the body.
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .WithDiscriminator(new CloudEventTypeDiscriminator())
                    .Handling<OrderPlaced>(placedType)
                    .Handling<OrderCancelled>(cancelledType))))
            .AddSingleton(placedHandler)
            .AddSingleton(cancelledHandler);

        // Serialize everything as CloudEvents (an all-CloudEvents app), mapping each type to its
        // CloudEvents `type`.
        services.AddJustSayingCloudEvents(options =>
        {
            options.Source = new Uri("https://orders.example.com");
            options.MapType<OrderPlaced>(placedType);
            options.MapType<OrderCancelled>(cancelledType);
        },
        useAsDefault: true);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-1" }, cancellationToken);
                await publisher.PublishAsync(new OrderCancelled { Reason = "out-of-stock" }, cancellationToken);

                // Assert
                (await placedHandled.Task.WaitAsync(cancellationToken)).OrderId.ShouldBe("order-1");
                (await cancelledHandled.Task.WaitAsync(cancellationToken)).Reason.ShouldBe("out-of-stock");
            });
    }

    [Test]
    public async Task Then_Native_And_CloudEvents_Types_Can_Share_The_Queue()
    {
        // Arrange - one queue carrying a native JustSaying message (routed by Subject, deserialized by
        // the app-wide default serializer) and a CloudEvent (routed by its `type`, deserialized by the
        // CloudEvents serializer). Each registration brings its own serializer, so neither format's
        // configuration disturbs the other.
        const string cancelledType = "com.example.orders.order.cancelled";

        var placedHandled = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelledHandled = new TaskCompletionSource<CloudEvent<OrderCancelled>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var placedHandler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        placedHandler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => placedHandled.TrySetResult(call.Arg<OrderPlaced>()));

        var cancelledHandler = Substitute.For<IHandlerAsync<CloudEvent<OrderCancelled>>>();
        cancelledHandler.Handle(Arg.Any<CloudEvent<OrderCancelled>>())
            .Returns(true)
            .AndDoes(call => cancelledHandled.TrySetResult(call.Arg<CloudEvent<OrderCancelled>>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                // The native publication uses the app-wide default (System.Text.Json) serializer.
                .Publications(p => p.WithQueue<OrderPlaced>(o => o.WithQueueName(UniqueName)))
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .Handling<OrderPlaced>()
                    .HandlingCloudEvent<OrderCancelled>(cancelledType))))
            .AddSingleton(placedHandler)
            .AddSingleton(cancelledHandler);

        // CloudEvents support for the HandlingCloudEvent registration only - the default serializer
        // (and with it the native publication and subscription) is untouched.
        services.AddJustSayingCloudEvents();

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act - publish the native message through the bus, and drop a structured CloudEvent
                // (as another CloudEvents system would produce it) straight onto the same queue.
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-1" }, cancellationToken);

                var sqs = CreateClientFactory().GetSqsClient(Region);
                var queueUrl = (await sqs.GetQueueUrlAsync(UniqueName, cancellationToken)).QueueUrl;
                var envelope = $$"""
                    {
                      "specversion": "1.0",
                      "id": "{{Guid.NewGuid()}}",
                      "source": "https://orders.example.com",
                      "type": "{{cancelledType}}",
                      "time": "{{DateTimeOffset.UtcNow:O}}",
                      "datacontenttype": "application/json",
                      "data": { "Reason": "out-of-stock" }
                    }
                    """;
                await sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = queueUrl, MessageBody = envelope }, cancellationToken);

                // Assert - each message reached the handler for its own type, in its own shape.
                (await placedHandled.Task.WaitAsync(cancellationToken)).OrderId.ShouldBe("order-1");

                var cancelled = await cancelledHandled.Task.WaitAsync(cancellationToken);
                cancelled.Data.Reason.ShouldBe("out-of-stock");
                cancelled.Type.ShouldBe(cancelledType);
                cancelled.Source.ShouldBe(new Uri("https://orders.example.com"));
            });
    }
}
