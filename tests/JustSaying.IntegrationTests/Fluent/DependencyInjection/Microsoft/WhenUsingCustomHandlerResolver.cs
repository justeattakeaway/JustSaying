using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.DependencyInjection.Microsoft;

public class WhenUsingCustomHandlerResolver(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Handler_Is_Resolved_From_The_Custom_Resolver()
    {
        // Arrange
        var future = new Future<OrderPlaced>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<OrderPlaced>(UniqueName + "-dispatched"))
            .ConfigureJustSaying((builder) => builder.Services((config) => config.WithHandlerResolver(new MyCustomHandlerResolver(future))));

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

    private sealed class MyCustomHandlerResolver : IHandlerResolver
    {
        internal MyCustomHandlerResolver(Future<OrderPlaced> future)
        {
            Future = future;
        }

        private Future<OrderPlaced> Future { get; }

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            if (typeof(T) == typeof(OrderPlaced) && context.QueueName.EndsWith("-dispatched", StringComparison.Ordinal))
            {
                return new OrderDispatcher(Future) as IHandlerAsync<T>;
            }

            throw new NotImplementedException();
        }
    }
}