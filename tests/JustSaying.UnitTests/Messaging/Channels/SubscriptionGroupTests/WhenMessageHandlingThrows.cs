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
        _queue.MaxNumberOfMessagesToReceive = 10;

        Queues.Add(sqsSource);

        Handler.OnHandle = (msg) =>
        {
            if (!_firstTime) return;

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
        // Avoid race condition
        await Task.Delay(TimeSpan.FromMilliseconds(250));
        await _queue.ReceivedAllMessages.WaitAsync(TimeSpan.FromSeconds(5));
        // Avoid race condition
        await Task.Delay(TimeSpan.FromMilliseconds(250));
        await CompletionMiddleware.Complete.WaitAsync(TimeSpan.FromSeconds(5));

        await Patiently.AssertThatAsync(() =>
        {
            Handler.ReceivedMessages.Count.ShouldBe(10);
            _queue.DeleteMessageRequests.Count.ShouldBe(9);
        });
    }

    [Fact]
    public void ExceptionIsLoggedToMonitor()
    {
        Monitor.HandledExceptions.ShouldNotBeEmpty();
    }
}
