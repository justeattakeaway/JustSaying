using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests;

public class WhenSqsIsSlow
{
    protected class TestMessage : Message { }

    private readonly MessageReceiveBuffer _messageReceiveBuffer;
    private readonly FakeSqsQueue _queue;

    public WhenSqsIsSlow(ITestOutputHelper testOutputHelper)
    {
        var loggerFactory = testOutputHelper.ToLoggerFactory();

        MiddlewareBase<ReceiveMessagesContext, IList<Message>> sqsMiddleware =
            new DelegateMiddleware<ReceiveMessagesContext, IList<Message>>();

        var messages = new List<Message> { new TestMessage() };
        _queue = new FakeSqsQueue(async ct =>
        {
            await Task.Delay(20, ct);
            return messages;
        })
        {
            MaxNumberOfMessagesToReceive = 10
        };

        var source = new SqsSource
        {
            SqsQueue = _queue,
            MessageConverter = new InboundMessageConverter(SimpleMessage.Serializer, new MessageCompressionRegistry(), false)
        };

        var monitor = new TrackingLoggingMonitor(
            loggerFactory.CreateLogger<TrackingLoggingMonitor>());

        _messageReceiveBuffer = new MessageReceiveBuffer(
            10,
            10,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            source,
            sqsMiddleware,
            null,
            monitor,
            loggerFactory.CreateLogger<IMessageReceiveBuffer>());
    }

    protected async Task<int> Messages()
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
    public async Task All_Messages_Are_Processed()
    {
        using var cts = new CancellationTokenSource();
        _ = _messageReceiveBuffer.RunAsync(cts.Token);
        var readTask = Messages();

        await _queue.ReceivedAllMessages.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);

        // Cancel token
        await cts.CancelAsync();

        // Ensure buffer completes
        await _messageReceiveBuffer.Reader.Completion;

        // Get the number of messages we read
        var messagesRead = await readTask;

        // Make sure that number makes sense
        messagesRead.ShouldBeGreaterThan(0);
        messagesRead.ShouldBeLessThanOrEqualTo(10);
    }
}
