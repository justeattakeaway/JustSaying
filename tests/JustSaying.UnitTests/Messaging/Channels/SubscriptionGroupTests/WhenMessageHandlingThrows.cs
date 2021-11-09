using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingThrows : BaseSubscriptionGroupTests
{
    private bool _firstTime = true;
    private FakeSqsQueue _queue;

    public WhenMessageHandlingThrows(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    { }

    protected override void Given()
    {
        _queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());

        Queues.Add(_queue);

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