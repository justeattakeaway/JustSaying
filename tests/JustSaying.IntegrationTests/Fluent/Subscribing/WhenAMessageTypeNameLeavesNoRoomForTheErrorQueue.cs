using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

/// <summary>
/// The naming convention names a queue after its message type, so a type name of 75 to 80 characters
/// gives a queue whose <c>_error</c> queue's name is longer than SQS allows. The error must say how to fix it.
/// </summary>
public class WhenAMessageTypeNameLeavesNoRoomForTheErrorQueue : IntegrationTestBase
{
    // 77 characters: a valid queue name, but not with "_error" appended.
    public sealed class AMessageTypeWhoseNameIsLongEnoughThatItsQueueNameLeavesNoRoomForTheErrorQueue
    {
    }

    private IServiceProvider GivenSubscription(Action<SubscriptionsBuilder> configure)
        => GivenJustSaying()
            .ConfigureJustSaying(builder => builder.Subscriptions(configure))
            .AddSingleton(Substitute.For<IHandlerAsync<AMessageTypeWhoseNameIsLongEnoughThatItsQueueNameLeavesNoRoomForTheErrorQueue>>())
            .BuildServiceProvider();

    [Test]
    public async Task Then_The_Error_Says_How_To_Fix_It()
    {
        // Arrange
        var serviceProvider = GivenSubscription(s => s.ForTopic<AMessageTypeWhoseNameIsLongEnoughThatItsQueueNameLeavesNoRoomForTheErrorQueue>());

        // Act
        var exception = Should.Throw<ConfigurationErrorsException>(() => serviceProvider.GetRequiredService<IMessagingBus>());

        // Assert
        exception.Message.ShouldContain("so the queue name can be at most 74");
        exception.Message.ShouldContain("QueueDestination.Named(...) or WithQueueName(...)");
        exception.Message.ShouldContain("WithNoErrorQueue()");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_Opting_Out_Of_The_Error_Queue_Fixes_It()
    {
        // Arrange
        var serviceProvider = GivenSubscription(s => s.ForQueue<AMessageTypeWhoseNameIsLongEnoughThatItsQueueNameLeavesNoRoomForTheErrorQueue>(
            QueueDestination.ByConvention(q => q.WithNoErrorQueue())));

        // Act and Assert
        serviceProvider.GetRequiredService<IMessagingBus>().ShouldNotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_Naming_The_Queue_Fixes_It()
    {
        // Arrange
        var serviceProvider = GivenSubscription(s => s.ForTopic<AMessageTypeWhoseNameIsLongEnoughThatItsQueueNameLeavesNoRoomForTheErrorQueue>(
            t => t.WithQueueName(UniqueName)));

        // Act and Assert
        serviceProvider.GetRequiredService<IMessagingBus>().ShouldNotBeNull();

        await Task.CompletedTask;
    }
}
