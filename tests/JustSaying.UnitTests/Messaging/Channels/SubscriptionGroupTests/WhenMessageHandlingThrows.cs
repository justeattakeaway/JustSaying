using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingThrows(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private bool _firstTime = true;
    private readonly object _firstTimeLock = new();
    private FakeSqsQueue _queue;

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue("TestQueue", new TestMessage());
        _queue = (FakeSqsQueue)sqsSource.SqsQueue;
        _queue.MaxNumberOfMessagesToReceive = 100;

        Queues.Add(sqsSource);

        Handler.OnHandle = (msg) =>
        {
            lock (_firstTimeLock)
            {
                if (!_firstTime) return;

                _firstTime = false;
                throw new TestException("Thrown by test handler");
            }
        };
    }

    [Fact]
    public void MessageHandlerWasCalled()
    {
        Handler.ReceivedMessages.Any(msg => msg.GetType() == typeof(SimpleMessage)).ShouldBeTrue();
    }

    [Fact]
    public async Task FailedMessageIsNotRemovedFromQueue()
    {
        await _queue.ReceivedAllMessages.WaitAsync(TimeSpan.FromSeconds(5));
        await CompletionMiddleware.Complete.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(1_500); // Give the handler a chance to run

        await Patiently.AssertThatAsync(() =>
        {
            Handler.ReceivedMessages.Count.ShouldBe(100);
            _queue.DeleteMessageRequests.Count.ShouldBe(99);
        });
    }

    [Fact]
    public void ExceptionIsLoggedToMonitor()
    {
        Monitor.HandledExceptions.ShouldNotBeEmpty();
    }
}
