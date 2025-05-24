using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenMessageHandlingThrows : BaseSubscriptionGroupTests
{
    private bool _firstTime = true;
    private readonly object _firstTimeLock = new();
    private FakeSqsQueue _queue;

    public WhenMessageHandlingThrows(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        MessagesToWaitFor = 10;
    }

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue("TestQueue", new TestMessage());
        _queue = (FakeSqsQueue)sqsSource.SqsQueue;
        _queue.MaxNumberOfMessagesToReceive = MessagesToWaitFor;

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

    protected override async Task<bool> UntilAsync()
    {
        await _queue.ReceivedAllMessages.WaitAsync(TimeSpan.FromSeconds(5));
        await CompletionMiddleware.Complete.WaitAsync(TimeSpan.FromSeconds(5));
        return await base.UntilAsync();
    }

    [Fact]
    public void MessageHandlerWasCalled()
    {
        Handler.ReceivedMessages.Any(msg => msg.GetType() == typeof(SimpleMessage)).ShouldBeTrue();
    }

    [Fact]
    public async Task FailedMessageIsNotRemovedFromQueue()
    {
        await Patiently.AssertThatAsync(() =>
        {
            OutputHelper.WriteLine($"HandledErrors: {Monitor.HandledErrors.Count}");
            OutputHelper.WriteLine($"ReceivedMessages: {Handler.ReceivedMessages.Count}");
            OutputHelper.WriteLine($"DeleteMessageRequests: {_queue.DeleteMessageRequests.Count}");

            Monitor.HandledErrors.Count.ShouldBe(1);
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
