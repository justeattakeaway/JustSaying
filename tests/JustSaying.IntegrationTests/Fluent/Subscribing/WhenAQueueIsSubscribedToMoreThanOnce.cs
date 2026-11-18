using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

/// <summary>
/// Subscriptions that share a queue compete for its messages, so each receives the other's and reads
/// them as the wrong type. Registering one queue twice must fail when the bus is built.
/// </summary>
public class WhenAQueueIsSubscribedToMoreThanOnce : IntegrationTestBase
{
    public sealed class OrderPlaced
    {
        public string OrderId { get; set; }
    }

    public sealed class ParcelShipped
    {
        public string TrackingId { get; set; }
    }

    private IServiceCollection GivenSubscriptions(Action<SubscriptionsBuilder> configure)
        => GivenJustSaying()
            .ConfigureJustSaying(builder => builder.Subscriptions(configure))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>())
            .AddSingleton(Substitute.For<IHandlerAsync<ParcelShipped>>());

    private InvalidOperationException ShouldFailToBuild(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        var exception = Should.Throw<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IMessagingBus>());

        exception.Message.ShouldContain($"'{UniqueName}'");
        exception.Message.ShouldContain(nameof(OrderPlaced));
        exception.Message.ShouldContain(nameof(ParcelShipped));
        exception.Message.ShouldContain($"ForQueue(\"{UniqueName}\", q => q.Handling<OrderPlaced>().Handling<ParcelShipped>())");

        return exception;
    }

    [Test]
    public async Task Then_Two_Queue_Subscriptions_Of_Different_Types_Fail()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(q => q.WithQueueName(UniqueName))
            .ForQueue<ParcelShipped>(q => q.WithQueueName(UniqueName)));

        // Act and Assert
        ShouldFailToBuild(services);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_Two_Topic_Subscriptions_Of_Different_Types_Sharing_A_Queue_Fail()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForTopic<OrderPlaced>(t => t.WithQueueName(UniqueName))
            .ForTopic<ParcelShipped>(t => t.WithQueueName(UniqueName)));

        // Act and Assert
        ShouldFailToBuild(services);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Single_Type_And_A_Multi_Type_Subscription_Sharing_A_Queue_Fail()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(q => q.WithQueueName(UniqueName))
            .ForQueue(UniqueName, q => q.Handling<ParcelShipped>()));

        // Act and Assert
        var exception = ShouldFailToBuild(services);
        exception.Message.ShouldContain("multi-type");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Queue_Subscribed_To_Two_Topics_Of_The_Same_Type_Is_Allowed()
    {
        // Arrange - both subscriptions read the same type, so sharing the queue is safe.
        var services = GivenSubscriptions(s => s
            .ForTopic<OrderPlaced>($"{UniqueName}-uk", t => t.WithQueueName(UniqueName))
            .ForTopic<OrderPlaced>($"{UniqueName}-ie", t => t.WithQueueName(UniqueName)));

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert
        serviceProvider.GetRequiredService<IMessagingBus>().ShouldNotBeNull();

        await Task.CompletedTask;
    }
}
