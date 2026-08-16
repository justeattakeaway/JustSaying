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

/// <summary>
/// Custom middleware registered via <c>WithCustomMiddleware</c> replaces
/// <see cref="DefaultReceiveMessagesMiddleware"/> entirely and may propagate the
/// <see cref="OperationCanceledException"/> from a pause-cancelled receive call. The buffer
/// must swallow that cancellation itself, or the channel completes and can never resume.
/// </summary>
public class WhenPausedDuringLongPollWithPassThroughMiddleware
{
    private class TestMessage : Message { }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly TaskCompletionSource<bool> _receiveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _receiveCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly MessageReceivePauseSignal _messageReceivePauseSignal;
    private readonly MessageReceiveBuffer _messageReceiveBuffer;

    public WhenPausedDuringLongPollWithPassThroughMiddleware(ITestOutputHelper testOutputHelper)
    {
        var loggerFactory = testOutputHelper.ToLoggerFactory();

        // Propagates all exceptions, including cancellation of the receive call
        MiddlewareBase<ReceiveMessagesContext, IList<Message>> sqsMiddleware =
            new DelegateMiddleware<ReceiveMessagesContext, IList<Message>>();

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
    public async Task Then_The_Buffer_Survives_And_Messages_Flow_Again_After_Resuming()
    {
        using var cts = new CancellationTokenSource();
        var _ = _messageReceiveBuffer.RunAsync(cts.Token);

        // Wait until a receive call is in flight, then pause
        (await Task.WhenAny(_receiveStarted.Task, Task.Delay(Timeout))).ShouldBe(_receiveStarted.Task);
        _messageReceivePauseSignal.Pause();

        (await Task.WhenAny(_receiveCancelled.Task, Task.Delay(Timeout))).ShouldBe(_receiveCancelled.Task);

        // The propagated cancellation must not have completed the channel
        _messageReceiveBuffer.Reader.Completion.IsCompleted.ShouldBeFalse();

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
