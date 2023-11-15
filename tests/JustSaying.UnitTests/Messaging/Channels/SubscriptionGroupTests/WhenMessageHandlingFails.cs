namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingFails(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private FakeSqsQueue _queue;

    protected override void Given()
    {
        _queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());

        Queues.Add(_queue);
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