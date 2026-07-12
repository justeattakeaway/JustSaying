using JustSaying.CloudEvents;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// End-to-end coverage for the CloudEvents message context: when a message arrives as a structured
/// CloudEvents envelope, a handler observes the envelope's context attributes through the injected
/// <see cref="IMessageContextReader"/> — the handler's own contract stays a plain
/// <see cref="IHandlerAsync{T}"/> of the data type.
/// </summary>
public class WhenReadingTheCloudEventMessageContext : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    private sealed class CaptureContextHandler(IMessageContextReader contextReader) : IHandlerAsync<OrderPlaced>
    {
        private readonly TaskCompletionSource<CloudEventMessageContext> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CloudEventMessageContext> Context => _completionSource.Task;

        public Task<bool> Handle(OrderPlaced message)
        {
            _completionSource.TrySetResult(contextReader.GetCloudEventContext());
            return Task.FromResult(true);
        }
    }

    [Test]
    public async Task Then_The_Handler_Observes_The_Envelope_Attributes()
    {
        // Arrange
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithQueue<OrderPlaced>(o => o.WithQueueName(UniqueName)))
                .Subscriptions(s => s.ForQueue<OrderPlaced>(sub => sub.WithQueueName(UniqueName))))
            .AddSingleton<CaptureContextHandler>()
            .AddSingleton<IHandlerAsync<OrderPlaced>>(p => p.GetRequiredService<CaptureContextHandler>());

        services.RemoveAll<IMessageBodySerializationFactory>();
        services.AddJustSayingCloudEvents(options =>
        {
            options.Source = new Uri("https://orders.example.com");
            options.WithCloudEventType<OrderPlaced>(OrderPlacedType);
        });

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var handler = serviceProvider.GetRequiredService<CaptureContextHandler>();

                // Act
                await publisher.PublishAsync(new OrderPlaced { OrderId = "order-42" }, cancellationToken);

                // Assert
                var context = await handler.Context.WaitAsync(cancellationToken);

                context.ShouldNotBeNull();
                context.SpecVersion.ShouldBe("1.0");
                context.Type.ShouldBe(OrderPlacedType);
                context.Source.ShouldBe(new Uri("https://orders.example.com/"));
                context.Id.ShouldNotBeNullOrEmpty();
                context.Time.ShouldNotBeNull();
                context.DataContentType.ShouldBe("application/json");

                // The base context members are populated too.
                context.QueueUri.AbsolutePath.ShouldEndWith(UniqueName);
                context.Message.ShouldNotBeNull();
            });
    }
}
