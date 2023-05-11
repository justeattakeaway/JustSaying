using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests;

public class WhenThereAreNoSubscribers
{
    protected class TestMessage : Message
    { }

    private int _callCount;
    private readonly MessageReceiveBuffer _messageReceiveBuffer;
    private readonly ITestOutputHelper _outputHelper;

    public WhenThereAreNoSubscribers(ITestOutputHelper testOutputHelper)
    {
        _outputHelper = testOutputHelper;
        var loggerFactory = testOutputHelper.ToLoggerFactory();

        MiddlewareBase<ReceiveMessagesContext, IList<Message>> sqsMiddleware =
            new DelegateMiddleware<ReceiveMessagesContext, IList<Message>>();

        var messages = new List<Message> { new TestMessage() };
        var queue = new FakeSqsQueue(ct =>
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(messages.AsEnumerable());
        });

        var monitor = new TrackingLoggingMonitor(
            loggerFactory.CreateLogger<TrackingLoggingMonitor>());

        _messageReceiveBuffer = new MessageReceiveBuffer(
            10,
            10,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            queue,
            sqsMiddleware,
            new MessageReceiveToggle(),
            TimeSpan.FromMilliseconds(100),
            monitor,
            loggerFactory.CreateLogger<IMessageReceiveBuffer>());
    }

    [Fact]
    public async Task Buffer_Is_Filled()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var _ = _messageReceiveBuffer.RunAsync(cts.Token);

        await Patiently.AssertThatAsync(_outputHelper, () => _callCount > 0);

        _callCount.ShouldBeGreaterThan(0);
    }
}
