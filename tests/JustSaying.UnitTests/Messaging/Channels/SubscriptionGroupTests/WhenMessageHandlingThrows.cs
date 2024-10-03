using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingThrows(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private bool _firstTime = true;
    private FakeSqsQueue _queue;

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue("TestQueue", new TestMessage());
        _queue = sqsSource.SqsQueue as FakeSqsQueue;

        Queues.Add(sqsSource);

        Handler.OnHandle = (msg) =>
        {
            if (!_firstTime) return;

            _firstTime = false;
            throw new TestException("Thrown by test handler");
        };
    }

    [Fact]
    public void MessageHandlerWasCalled()
    {
        Handler.ReceivedMessages.Any(msg => msg.GetType() == typeof(SimpleMessage)).ShouldBeTrue();
    }

    [Fact]
    public void FailedMessageIsNotRemovedFromQueue()
    {
        var numberHandled = Handler.ReceivedMessages.Count;

        _queue.DeleteMessageRequests.Count.ShouldBe(numberHandled - 1);
    }

    [Fact]
    public void ExceptionIsLoggedToMonitor()
    {
        Monitor.HandledExceptions.ShouldNotBeEmpty();
    }
}
