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

    [Test]
    public async Task Then_A_Message_Of_An_Unregistered_Type_Is_Redriven_To_The_Error_Queue()
    {
        // Arrange - the producer ships OrderCancelled before this consumer handles it. The message must
        // not be deleted: it goes to the error queue, where it can be redriven once the consumer catches up.
        var placedHandled = new TaskCompletionSource<OrderPlaced>(TaskCreationOptions.RunContinuationsAsynchronously);

        var placedHandler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        placedHandler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call => placedHandled.TrySetResult(call.Arg<OrderPlaced>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p =>
                {
                    p.WithQueue<OrderPlaced>(o => o.WithQueueName(UniqueName));
                    p.WithQueue<OrderCancelled>(o => o.WithQueueName(UniqueName));
                })
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .WithReadConfiguration(c =>
                    {
                        c.VisibilityTimeout = TimeSpan.FromSeconds(1);
                        c.RetryCountBeforeSendingToErrorQueue = 1;
                    })
                    .Handling<OrderPlaced>())))
            .AddSingleton(placedHandler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new OrderCancelled { Reason = "out-of-stock" }, cancellationToken);
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-1" }, cancellationToken);

                // Assert - the registered type is still handled, and the unregistered one is dead-lettered.
                (await placedHandled.Task.WaitAsync(cancellationToken)).OrderId.ShouldBe("order-1");

                var sqs = CreateClientFactory().GetSqsClient(Region);
                var errorQueueUrl = (await sqs.GetQueueUrlAsync($"{UniqueName}_error", cancellationToken)).QueueUrl;

                List<Amazon.SQS.Model.Message> deadLettered = [];
                while (deadLettered.Count == 0)
                {
                    var received = await sqs.ReceiveMessageAsync(
                        new Amazon.SQS.Model.ReceiveMessageRequest { QueueUrl = errorQueueUrl, WaitTimeSeconds = 1 },
                        cancellationToken);
                    deadLettered.AddRange(received.Messages ?? []);
                }

                deadLettered.ShouldHaveSingleItem().Body.ShouldContain("out-of-stock");
            });
    }

    [Test]
    public async Task Then_Two_Types_Resolving_To_The_Same_Name_Fail_Fast()
    {
        // Arrange - the type name is what routes an inbound message to a serializer, so a duplicate
        // would silently overwrite one registration and deserialize messages as the wrong type.
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .Handling<OrderPlaced>("orders")
                    .Handling<OrderCancelled>("orders"))))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>())
            .AddSingleton(Substitute.For<IHandlerAsync<OrderCancelled>>());

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var exception = Should.Throw<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IMessagingBus>());

        // Assert
        exception.Message.ShouldContain(nameof(OrderPlaced));
        exception.Message.ShouldContain(nameof(OrderCancelled));
        exception.Message.ShouldContain("orders");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_Raw_Delivery_With_Subject_Routing_Fails_Fast()
    {
        // Arrange - raw messages carry no Subject, so this queue could never route a single message.
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .WithReadConfiguration(c => c.RawMessageDelivery = true)
                    .Handling<OrderPlaced>())))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>());

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var exception = Should.Throw<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IMessagingBus>());

        // Assert
        exception.Message.ShouldContain(UniqueName);
        exception.Message.ShouldContain("raw message delivery");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_Raw_Delivery_With_A_Body_Discriminator_Is_Allowed()
    {
        // Arrange - the CloudEvents type is in the body, so raw delivery can still be routed.
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .WithReadConfiguration(c => c.RawMessageDelivery = true)
                    .WithDiscriminator(new CloudEventTypeDiscriminator())
                    .Handling<OrderPlaced>("com.example.orders.order.placed"))))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>());

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert
        serviceProvider.GetRequiredService<IMessagingBus>().ShouldNotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Blank_Type_Name_Fails_Fast()
    {
        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForQueue(UniqueName, q => q
                    .Handling<OrderPlaced>("   "))))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>());

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var exception = Should.Throw<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IMessagingBus>());

        // Assert
        exception.Message.ShouldContain(nameof(OrderPlaced));

        await Task.CompletedTask;
    }
}
