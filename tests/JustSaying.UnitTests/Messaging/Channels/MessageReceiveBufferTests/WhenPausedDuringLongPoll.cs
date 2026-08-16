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

public class WhenPausedDuringLongPoll
{
    private class TestMessage : Message { }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly TaskCompletionSource<bool> _receiveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _receiveCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly MessageReceivePauseSignal _messageReceivePauseSignal;
    private readonly MessageReceiveBuffer _messageReceiveBuffer;

    public WhenPausedDuringLongPoll(ITestOutputHelper testOutputHelper)
    {
        var loggerFactory = testOutputHelper.ToLoggerFactory();

        MiddlewareBase<ReceiveMessagesContext, IList<Message>> sqsMiddleware =
            new DefaultReceiveMessagesMiddleware(loggerFactory.CreateLogger<DefaultReceiveMessagesMiddleware>());

        var messages = new List<Message> { new TestMessage() };
        var queue = new FakeSqsQueue(async ct =>
        {
            _receiveStarted.TrySetResult(true);

            // The first receive long polls until it is cancelled; once that has
            // happened, subsequent receives return messages immediately
            if (!_receiveCancelled.Task.IsCompleted)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), ct);
                }
                catch (OperationCanceledException)
                {
                    _receiveCancelled.TrySetResult(true);
                    throw;
                }
            }

            return messages.AsEnumerable();
        });

        var source = new SqsSource
        {
            SqsQueue = queue,
            MessageConverter = new InboundMessageConverter(SimpleMessage.Serializer, new MessageCompressionRegistry(), false)
        };

        _messageReceivePauseSignal = new MessageReceivePauseSignal();

        var monitor = new TrackingLoggingMonitor(
            loggerFactory.CreateLogger<TrackingLoggingMonitor>());

        _messageReceiveBuffer = new MessageReceiveBuffer(
            10,
            10,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(20),
            source,
            sqsMiddleware,
            _messageReceivePauseSignal,
            monitor,
            loggerFactory.CreateLogger<IMessageReceiveBuffer>());
    }

    [Fact]
    public async Task Then_The_In_Flight_Receive_Call_Is_Cancelled()
    {
        using var cts = new CancellationTokenSource();
        var _ = _messageReceiveBuffer.RunAsync(cts.Token);

        // Wait until a receive call is in flight, then pause
        (await Task.WhenAny(_receiveStarted.Task, Task.Delay(Timeout))).ShouldBe(_receiveStarted.Task);
        _messageReceivePauseSignal.Pause();

        // The in-flight receive call should be cancelled promptly, rather than
        // running for the full wait time
        (await Task.WhenAny(_receiveCancelled.Task, Task.Delay(Timeout))).ShouldBe(_receiveCancelled.Task);

        // Nothing should have been buffered
        _messageReceiveBuffer.Reader.TryRead(out var _).ShouldBeFalse();

        await cts.CancelAsync();
        await _messageReceiveBuffer.Reader.Completion;
    }

    [Fact]
    public async Task Then_Messages_Flow_Again_After_Resuming()
    {
        using var cts = new CancellationTokenSource();
        var _ = _messageReceiveBuffer.RunAsync(cts.Token);

        // Wait until a receive call is in flight, then pause
        (await Task.WhenAny(_receiveStarted.Task, Task.Delay(Timeout))).ShouldBe(_receiveStarted.Task);
        _messageReceivePauseSignal.Pause();

        (await Task.WhenAny(_receiveCancelled.Task, Task.Delay(Timeout))).ShouldBe(_receiveCancelled.Task);

        _messageReceivePauseSignal.Resume();

        using var readTimeout = new CancellationTokenSource(Timeout);
        var couldRead = await _messageReceiveBuffer.Reader.WaitToReadAsync(readTimeout.Token);
        couldRead.ShouldBeTrue();

        await cts.CancelAsync();

        // Drain any buffered messages so the channel can complete
        while (await _messageReceiveBuffer.Reader.WaitToReadAsync())
        {
            _messageReceiveBuffer.Reader.TryRead(out var _);
        }

        await _messageReceiveBuffer.Reader.Completion;
    }
}
