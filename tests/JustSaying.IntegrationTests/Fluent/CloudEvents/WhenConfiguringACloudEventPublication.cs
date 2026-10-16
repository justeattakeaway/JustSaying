using System.Collections.Concurrent;
using JustSaying.CloudEvents;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.CloudEvents;

/// <summary>
/// A CloudEvents publication registers two publications (the bare <c>T</c> and the
/// <see cref="CloudEvent{T}"/> envelope); its configure callback must reach both.
/// </summary>
public class WhenConfiguringACloudEventPublication : IntegrationTestBase
{
    private const string OrderPlacedType = "com.example.orders.order.placed";
    private static readonly Uri Source = new("/orders", UriKind.Relative);

    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    [Test]
    public async Task Then_A_Topic_Publications_Configuration_Applies_To_Both_Shapes()
    {
        var published = new ConcurrentQueue<Type>();

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventTopic<OrderPlaced>(
                    OrderPlacedType,
                    Source,
                    configure: t => t
                        .WithTopicName(UniqueName)
                        .WithMiddlewareConfiguration(m => m.Use(new RecordingMiddleware(published))))));

        await PublishBothShapesAsync(services);

        published.ShouldBe([typeof(OrderPlaced), typeof(CloudEvent<OrderPlaced>)]);
    }

    [Test]
    public async Task Then_A_Queue_Publications_Configuration_Applies_To_Both_Shapes()
    {
        var published = new ConcurrentQueue<Type>();

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithCloudEventQueue<OrderPlaced>(
                    QueueDestination.Named(UniqueName),
                    OrderPlacedType,
                    Source,
                    q => q.WithMiddlewareConfiguration(m => m.Use(new RecordingMiddleware(published))))));

        await PublishBothShapesAsync(services);

        published.ShouldBe([typeof(OrderPlaced), typeof(CloudEvent<OrderPlaced>)]);
    }

    private async Task PublishBothShapesAsync(IServiceCollection services)
    {
        services.AddJustSayingCloudEvents();

        var publisher = services.BuildServiceProvider().GetRequiredService<IMessagePublisher>();

        await RunActionWithTimeout(async cancellationToken =>
        {
            await publisher.StartAsync(cancellationToken);

            await publisher.PublishAsync(new OrderPlaced { OrderId = "bare-1" }, cancellationToken);
            await publisher.PublishAsync(new CloudEvent<OrderPlaced>(new OrderPlaced { OrderId = "wrapped-2" }), cancellationToken);
        });
    }

    private sealed class RecordingMiddleware(ConcurrentQueue<Type> published) : MiddlewareBase<PublishContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(PublishContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            published.Enqueue(context.Message.GetType());
            return await func(stoppingToken);
        }
    }
}
