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

    private string QueueUrl => $"https://sqs.{RegionName}.amazonaws.com/123456789012/{UniqueName}";

    private string QueueArn => $"arn:aws:sqs:{RegionName}:123456789012:{UniqueName}";

    private IServiceCollection GivenSubscriptions(Action<SubscriptionsBuilder> configure)
        => GivenJustSaying()
            .ConfigureJustSaying(builder => builder.Subscriptions(configure))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderPlaced>>())
            .AddSingleton(Substitute.For<IHandlerAsync<ParcelShipped>>());

    private InvalidOperationException ShouldFailToBuild(IServiceCollection services, string expectedFix = null)
    {
        var serviceProvider = services.BuildServiceProvider();

        var exception = Should.Throw<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IMessagingBus>());

        exception.Message.ShouldContain($"'{UniqueName}'");
        exception.Message.ShouldContain(nameof(OrderPlaced));
        exception.Message.ShouldContain(nameof(ParcelShipped));
        exception.Message.ShouldContain(expectedFix ?? $"ForQueue(\"{UniqueName}\", q => q.Handling<OrderPlaced>().Handling<ParcelShipped>())");

        return exception;
    }

    private void ShouldBuild(IServiceCollection services)
        => services.BuildServiceProvider().GetRequiredService<IMessagingBus>().ShouldNotBeNull();

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
    public async Task Then_Two_Topic_Subscriptions_Of_Different_Types_Sharing_A_Queue_Fail_Suggesting_The_Topics()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForTopic<OrderPlaced>(t => t.WithQueueName(UniqueName))
            .ForTopic<ParcelShipped>($"{UniqueName}-parcels", t => t.WithQueueName(UniqueName)));

        // Act and Assert - the fix subscribes the multi-type queue to both topics, by convention where
        // the topic was named by convention.
        var exception = ShouldFailToBuild(
            services,
            $"ForQueue(\"{UniqueName}\", q => q.Handling<OrderPlaced>().Handling<ParcelShipped>()" +
            $".SubscribeToTopic<OrderPlaced>().SubscribeToTopic(TopicDestination.Named(\"{UniqueName}-parcels\")))");

        exception.Message.ShouldContain("'OrderPlaced' from topic 'orderplaced'");

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
    public async Task Then_A_Topic_Subscription_Into_A_Multi_Type_Queue_Fails()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue(UniqueName, q => q.Handling<OrderPlaced>().Handling<ParcelShipped>())
            .ForTopic<ParcelShipped>(t => t.WithQueueName(UniqueName)));

        // Act and Assert
        ShouldFailToBuild(
            services,
            $"ForQueue(\"{UniqueName}\", q => q.Handling<OrderPlaced>().Handling<ParcelShipped>().SubscribeToTopic<ParcelShipped>())");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Queue_Named_And_Addressed_By_Url_Fails()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(QueueDestination.Named(UniqueName))
            .ForQueue<ParcelShipped>(QueueDestination.FromUrl(QueueUrl)));

        // Act and Assert
        ShouldFailToBuild(services);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Queue_Named_And_Addressed_By_A_Url_Without_A_Region_Fails()
    {
        // Arrange - a local emulator's URL doesn't say its region, so it is taken to be the bus region.
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(QueueDestination.Named(UniqueName))
            .ForQueue<ParcelShipped>(QueueDestination.FromUrl($"http://localhost:4566/000000000000/{UniqueName}")));

        // Act and Assert
        ShouldFailToBuild(services);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Queue_Addressed_By_Url_And_By_Arn_Fails()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(QueueDestination.FromUrl(QueueUrl))
            .ForQueue<ParcelShipped>(QueueDestination.FromArn(QueueArn)));

        // Act and Assert
        ShouldFailToBuild(
            services,
            $"ForQueue(QueueDestination.FromUrl(\"{QueueUrl}\"), q => q.Handling<OrderPlaced>().Handling<ParcelShipped>())");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Queue_Of_The_Same_Name_In_Another_Account_Is_Allowed()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(QueueDestination.FromArn(QueueArn))
            .ForQueue<ParcelShipped>(QueueDestination.FromArn($"arn:aws:sqs:{RegionName}:210987654321:{UniqueName}")));

        // Act and Assert
        ShouldBuild(services);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Queue_Of_The_Same_Name_In_Another_Region_Is_Allowed()
    {
        // Arrange
        var otherRegion = RegionName == "eu-west-1" ? "eu-west-2" : "eu-west-1";
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(QueueDestination.Named(UniqueName))
            .ForQueue(QueueDestination.FromArn($"arn:aws:sqs:{otherRegion}:123456789012:{UniqueName}"), q => q.Handling<ParcelShipped>()));

        // Act and Assert
        ShouldBuild(services);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Queue_Subscribed_To_Two_Topics_Of_The_Same_Type_Is_Allowed()
    {
        // Arrange - both subscriptions read the same type, so sharing the queue is safe.
        var services = GivenSubscriptions(s => s
            .ForTopic<OrderPlaced>($"{UniqueName}-uk", t => t.WithQueueName(UniqueName))
            .ForTopic<OrderPlaced>($"{UniqueName}-ie", t => t.WithQueueName(UniqueName)));

        // Act and Assert
        ShouldBuild(services);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_The_Same_Topic_Subscription_Twice_Fails()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForTopic<OrderPlaced>()
            .ForTopic<OrderPlaced>());

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IMessagingBus>());

        exception.Message.ShouldBe(
            "The queue 'orderplaced' is subscribed to more than once by 'OrderPlaced' from topic 'orderplaced'. Remove the duplicate registration.");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_The_Same_Queue_Subscription_Twice_Fails()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue<OrderPlaced>(QueueDestination.Named(UniqueName))
            .ForQueue<OrderPlaced>(QueueDestination.FromUrl(QueueUrl)));

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagingBus>())
            .Message.ShouldContain("Remove the duplicate registration.");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_A_Multi_Type_Queue_Subscribed_To_The_Same_Topic_Twice_Fails()
    {
        // Arrange
        var services = GivenSubscriptions(s => s
            .ForQueue(UniqueName, q => q
                .Handling<OrderPlaced>()
                .SubscribeToTopic<OrderPlaced>()
                .SubscribeToTopic(TopicDestination.Named("orderplaced"))));

        var serviceProvider = services.BuildServiceProvider();

        // Act and Assert
        Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagingBus>())
            .Message.ShouldContain("subscribes to the topic 'orderplaced' more than once");

        await Task.CompletedTask;
    }
}
