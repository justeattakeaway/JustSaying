namespace JustSaying.Benchmark;

public class MessageMetric(Guid messageId, long ackLatency, long consumeLatency)
{
    public Guid MessageId { get; } = messageId;
    public long AckLatency { get; set; } = ackLatency;
    public long ConsumeLatency { get; set; } = consumeLatency;
}
