using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingFails(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private FakeSqsQueue _queue;

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());
        _queue = sqsSource.SqsQueue as FakeSqsQueue;

        Queues.Add(sqsSource);
        Handler.ShouldSucceed = false;
    }

    [Fact]
    public void MessageHandlerWasCalled()
    {
        Handler.ReceivedMessages.ShouldNotBeEmpty();
    }

    [Fact]
    public void FailedMessageIsNotRemovedFromQueue()
    {
        _queue.DeleteMessageRequests.ShouldBeEmpty();
    }

    [Fact]
    public void ExceptionIsNotLoggedToMonitor()
    {
        Monitor.HandledExceptions.ShouldBeEmpty();
    }
}
