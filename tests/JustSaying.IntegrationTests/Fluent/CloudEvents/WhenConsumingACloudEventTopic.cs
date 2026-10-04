using System.Collections.Concurrent;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// The subscribe-side counterpart of <c>WithCloudEventTopic&lt;T&gt;</c>: one service publishes
/// CloudEvents to a topic, and another consumes them with <c>ForCloudEventTopic&lt;T&gt;</c> (the
/// handler receives the envelope) or <c>ForCloudEventTopicData&lt;T&gt;</c> (the bare payload). Both
/// sides name the topic after <c>T</c>, so they meet with no names configured.
/// </summary>
public class WhenConsumingACloudEventTopic : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_The_Envelope_Handler_Receives_Both_Published_Shapes()
    {
        // Arrange
        var handled = new ConcurrentQueue<CloudEvent<OrderPlaced>>();
        var bothHandled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<CloudEvent<OrderPlaced>>>();
        handler.Handle(Arg.Any<CloudEvent<OrderPlaced>>())
            .Returns(true)
            .AndDoes(call =>
            {
                handled.Enqueue(call.Arg<CloudEvent<OrderPlaced>>());
                if (handled.Count == 2)
                {
                    bothHandled.TrySetResult();
                }
            });

        var consumer = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForCloudEventTopic<OrderPlaced>(OrderPlacedType, t => t.WithQueueName(UniqueName))))
            .AddSingleton(handler);
        consumer.AddJustSayingCloudEvents();

        // Act
        await PublishFromAnotherServiceAsync(consumer, bothHandled.Task);

        // Assert
        var bare = handled.Single(e => e.Data.OrderId == "bare-1");
        bare.Type.ShouldBe(OrderPlacedType);
        bare.Source.ShouldBe(new Uri("/orders", UriKind.Relative));

        var wrapped = handled.Single(e => e.Data.OrderId == "wrapped-2");
        wrapped.Subject.ShouldBe("orders/2");
        wrapped.Extensions["tenantid"].ShouldBe("acme");
    }

    [Test]
    public async Task Then_The_Data_Handler_Receives_The_Bare_Payload()
    {
        // Arrange
        var handled = new ConcurrentQueue<OrderPlaced>();
        var bothHandled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<OrderPlaced>>();
        handler.Handle(Arg.Any<OrderPlaced>())
            .Returns(true)
            .AndDoes(call =>
            {
                handled.Enqueue(call.Arg<OrderPlaced>());
                if (handled.Count == 2)
                {
                    bothHandled.TrySetResult();
                }
            });

        var consumer = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Subscriptions(s => s.ForCloudEventTopicData<OrderPlaced>(OrderPlacedType, t => t.WithQueueName(UniqueName))))
            .AddSingleton(handler);
        consumer.AddJustSayingCloudEvents();

        // Act
        await PublishFromAnotherServiceAsync(consumer, bothHandled.Task);

        // Assert
        handled.Select(message => message.OrderId).OrderBy(id => id, StringComparer.Ordinal).ShouldBe(["bare-1", "wrapped-2"]);
    }

    private async Task PublishFromAnotherServiceAsync(IServiceCollection consumer, Task handled)
    {
        var producer = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventTopic<OrderPlaced>(OrderPlacedType, new Uri("/orders", UriKind.Relative))));
        producer.AddJustSayingCloudEvents();

        var publisher = producer.BuildServiceProvider().GetRequiredService<IMessagePublisher>();
        var listener = consumer.BuildServiceProvider().GetRequiredService<IMessagingBus>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await listener.StartAsync(cancellationToken);
            await publisher.StartAsync(cancellationToken);

            await publisher.PublishAsync(new OrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<OrderPlaced>(
                new OrderPlaced { OrderId = "wrapped-2" },
                subject: "orders/2",
                extensions: new Dictionary<string, string> { ["tenantid"] = "acme" }), cancellationToken);

            await handled.WaitAsync(cancellationToken);
        });
    }
}
