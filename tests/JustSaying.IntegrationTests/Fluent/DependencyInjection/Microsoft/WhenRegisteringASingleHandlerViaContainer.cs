using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.DependencyInjection.Microsoft;

public class WhenRegisteringASingleHandlerViaContainer(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Handler_Is_Resolved()
    {
        // Arrange
        var future = new Future<OrderPlaced>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<OrderPlaced>(UniqueName))
            .AddTransient<IHandlerAsync<OrderPlaced>, OrderProcessor>()
            .AddSingleton(future);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var message = new OrderPlaced(Guid.NewGuid().ToString());

                // Act
                await publisher.PublishAsync(message, cancellationToken);

                //Assert
                await future.DoneSignal;
                future.ReceivedMessageCount.ShouldBeGreaterThan(0);
            });
    }

    [AwsFact]
    public async Task Then_The_Handler_Is_Resolved_ForMultiMessage()
    {
        // Arrange
        var future = new Future<OrderPlaced>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<OrderPlaced>(UniqueName))
            .AddTransient<IHandlerAsync<OrderPlaced>, OrderProcessor>()
            .AddSingleton(future);

        await WhenBatchAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                future.ExpectedMessageCount = 10;
                var messages = new List<Message>();

                for (int i = 0; i < future.ExpectedMessageCount; i++)
                {
                    messages.Add(new OrderPlaced(Guid.NewGuid().ToString()));
                }

                // Act
                await publisher.PublishAsync(messages, cancellationToken);

                //Assert
                await future.DoneSignal;
                future.ReceivedMessageCount.ShouldBeGreaterThan(2);
            });
    }
}
