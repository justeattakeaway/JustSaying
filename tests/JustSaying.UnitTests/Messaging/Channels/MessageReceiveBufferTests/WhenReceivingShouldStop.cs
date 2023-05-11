using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests;

public class WhenReceivingShouldStop
{
    private class TestMessage : Message { }

    private int _callCount;
    private readonly IMessageReceiveToggle _messageReceiveToggle;
    private readonly MessageReceiveBuffer _messageReceiveBuffer;

    public WhenReceivingShouldStop(ITestOutputHelper testOutputHelper)
    {
        var loggerFactory = testOutputHelper.ToLoggerFactory();

        MiddlewareBase<ReceiveMessagesContext, IList<Message>> sqsMiddleware =
            new DelegateMiddleware<ReceiveMessagesContext, IList<Message>>();

        var messages = new List<Message> { new TestMessage() };
        var queue = new FakeSqsQueue(ct =>
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(messages.AsEnumerable());
        });

        _messageReceiveToggle = new MessageReceiveToggle();

        var monitor = new TrackingLoggingMonitor(
            loggerFactory.CreateLogger<TrackingLoggingMonitor>());

        _messageReceiveBuffer = new MessageReceiveBuffer(
            10,
            10,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            queue,
            sqsMiddleware,
            _messageReceiveToggle,
            TimeSpan.FromMilliseconds(100),
            monitor,
            loggerFactory.CreateLogger<IMessageReceiveBuffer>());
    }

    private async Task<int> Messages()
    {
        int messagesProcessed = 0;

        while (true)
        {
            var couldRead = await _messageReceiveBuffer.Reader.WaitToReadAsync();
            if (!couldRead) break;

            while (_messageReceiveBuffer.Reader.TryRead(out var _))
            {
                messagesProcessed++;
            }
        }

        return messagesProcessed;
    }

    [Fact]
    public async Task No_Messages_Are_Processed()
    {
        // Signal stop receiving messages
        _messageReceiveToggle.Stop();

        using var cts = new CancellationTokenSource();
        var _ = _messageReceiveBuffer.RunAsync(cts.Token);
        var readTask = Messages();

        // Check if we can start receiving for a while
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Cancel token
        cts.Cancel();

        // Ensure buffer completes
        await _messageReceiveBuffer.Reader.Completion;

        // Get the number of messages we read
        var messagesRead = await readTask;

        // Make sure that number makes sense
        messagesRead.ShouldBe(0);
        messagesRead.ShouldBeLessThanOrEqualTo(_callCount);
    }

    [Fact]
    public async Task All_Message_Are_Processed_After_Starting()
    {
        // Signal stop receiving messages
        _messageReceiveToggle.Stop();

        using var cts = new CancellationTokenSource();
        var _ = _messageReceiveBuffer.RunAsync(cts.Token);
        var readTask = Messages();

        // Check if we can start receiving for a while
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Signal start receiving messages
        _messageReceiveToggle.Start();

        // Read messages for a while
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Cancel token
        cts.Cancel();

        // Ensure buffer completes
        await _messageReceiveBuffer.Reader.Completion;

        // Get the number of messages we read
        var messagesRead = await readTask;

        // Make sure that number makes sense
        messagesRead.ShouldBeGreaterThan(0);
        messagesRead.ShouldBeLessThanOrEqualTo(_callCount);
    }
}
