using System.Collections.Concurrent;
using System.Diagnostics;

namespace JustSaying.Benchmark;

// Borrowed with ❤ from https://github.com/MassTransit/MassTransit-Benchmark/blob/a04a0235e1/src/MassTransit-Benchmark/Latency/MessageMetricCapture.cs
public class MessageMetricCapture(long messageCount) : IReportConsumerMetric
{
    private readonly TaskCompletionSource<TimeSpan> _consumeCompleted = new();
    private readonly ConcurrentBag<ConsumedMessage> _consumedMessages = [];
    private readonly TaskCompletionSource<TimeSpan> _sendCompleted = new();
    private readonly ConcurrentBag<SentMessage> _sentMessages = [];
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _consumed;
    private long _sent;

    public Task<TimeSpan> SendCompleted => _sendCompleted.Task;
    public Task<TimeSpan> ConsumeCompleted => _consumeCompleted.Task;

    Task IReportConsumerMetric.Consumed<T>(Guid messageId)
    {
        _consumedMessages.Add(new ConsumedMessage(messageId, _stopwatch.ElapsedTicks));

        long consumed = Interlocked.Increment(ref _consumed);
        if (consumed == messageCount)
            _consumeCompleted.TrySetResult(_stopwatch.Elapsed);

        return Task.CompletedTask;
    }

    public async Task Sent(Guid messageId, Task sendTask)
    {
        long sendTimestamp = _stopwatch.ElapsedTicks;

        await sendTask.ConfigureAwait(false);

        long ackTimestamp = _stopwatch.ElapsedTicks;

        _sentMessages.Add(new SentMessage(messageId, sendTimestamp, ackTimestamp));

        long sent = Interlocked.Increment(ref _sent);
        if (sent == messageCount)
            _sendCompleted.TrySetResult(_stopwatch.Elapsed);
    }

    public MessageMetric[] GetMessageMetrics()
    {
        return _sentMessages.Join(_consumedMessages,
                x => x.MessageId,
                x => x.MessageId,
                (sent, consumed) =>
                    new MessageMetric(sent.MessageId,
                        sent.AckTimestamp - sent.SendTimestamp,
                        consumed.Timestamp - sent.SendTimestamp))
            .ToArray();
    }

    private readonly struct SentMessage(Guid messageId, long sendTimestamp, long ackTimestamp)
    {
        public readonly Guid MessageId = messageId;
        public readonly long SendTimestamp = sendTimestamp;
        public readonly long AckTimestamp = ackTimestamp;
    }

    private readonly struct ConsumedMessage(Guid messageId, long timestamp)
    {
        public readonly Guid MessageId = messageId;
        public readonly long Timestamp = timestamp;
    }
}
