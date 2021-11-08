using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.DependencyInjection.Microsoft;

public class WhenRegisteringASingleHandlerViaContainer : IntegrationTestBase
{
    public WhenRegisteringASingleHandlerViaContainer(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

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
}