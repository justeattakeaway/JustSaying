using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

/// <summary>
/// Proves the v9 "drop the <c>Message</c> constraint" change end-to-end: a message type that does
/// not derive from <see cref="JustSaying.Models.Message"/> can be published and handled through the
/// full in-memory publish -> subscribe -> handle round trip.
/// </summary>
public class WhenPublishingAMessageNotDerivingFromMessage : IntegrationTestBase
{
    // A POCO message that deliberately does NOT derive from JustSaying.Models.Message.
    public sealed class OrderPlacedPoco
    {
        public string OrderId { get; set; }
    }

    private sealed class OrderPlacedPocoHandler(TaskCompletionSource<OrderPlacedPoco> completionSource) : IHandlerAsync<OrderPlacedPoco>
    {
        public Task<bool> Handle(OrderPlacedPoco message)
        {
            completionSource.TrySetResult(message);
            return Task.FromResult(true);
        }
    }

    [Test]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<OrderPlacedPoco>(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<OrderPlacedPoco>>();
        handler.Handle(Arg.Any<OrderPlacedPoco>())
            .Returns(true)
            .AndDoes(call => completionSource.TrySetResult(call.Arg<OrderPlacedPoco>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithQueue<OrderPlacedPoco>(o => o.WithQueueName(UniqueName)))
                .Subscriptions(s => s.ForQueue<OrderPlacedPoco>(sub => sub.WithQueueName(UniqueName))))
            .AddSingleton(handler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new OrderPlacedPoco { OrderId = "abc-123" }, cancellationToken);

                // Assert
                var handled = await completionSource.Task.WaitAsync(cancellationToken);
                handled.OrderId.ShouldBe("abc-123");
            });
    }

    [Test]
    public async Task Then_The_Message_Is_Handled_When_Registered_Via_AddJustSayingHandler()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<OrderPlacedPoco>(TaskCreationOptions.RunContinuationsAsynchronously);

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithQueue<OrderPlacedPoco>(o => o.WithQueueName(UniqueName)))
                .Subscriptions(s => s.ForQueue<OrderPlacedPoco>(sub => sub.WithQueueName(UniqueName))))
            .AddSingleton(completionSource)
            .AddJustSayingHandler<OrderPlacedPoco, OrderPlacedPocoHandler>();

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new OrderPlacedPoco { OrderId = "def-456" }, cancellationToken);

                // Assert
                var handled = await completionSource.Task.WaitAsync(cancellationToken);
                handled.OrderId.ShouldBe("def-456");
            });
    }
}
