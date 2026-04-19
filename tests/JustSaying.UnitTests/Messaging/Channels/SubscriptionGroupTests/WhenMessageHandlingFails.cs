using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingFails : BaseSubscriptionGroupTests
{
    private FakeSqsQueue _queue;

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());
        _queue = sqsSource.SqsQueue as FakeSqsQueue;

        Queues.Add(sqsSource);
        Handler.ShouldSucceed = false;
    }

    [Test]
    public void MessageHandlerWasCalled()
    {
        Handler.ReceivedMessages.ShouldNotBeEmpty();
    }

    [Test]
    public void FailedMessageIsNotRemovedFromQueue()
    {
        _queue.DeleteMessageRequests.ShouldBeEmpty();
    }

    [Test]
    public void ExceptionIsNotLoggedToMonitor()
    {
        Monitor.HandledExceptions.ShouldBeEmpty();
    }
}
