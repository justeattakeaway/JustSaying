using JustSaying.CloudEvents;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // Serialize everything as CloudEvents, mapping each type to its CloudEvents `type`.
        services.RemoveAll<IMessageBodySerializationFactory>();
        services.AddJustSayingCloudEvents(options =>
        {
            options.Source = new Uri("https://orders.example.com");
            options.WithCloudEventType<OrderPlaced>(placedType);
            options.WithCloudEventType<OrderCancelled>(cancelledType);
        });

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
}
