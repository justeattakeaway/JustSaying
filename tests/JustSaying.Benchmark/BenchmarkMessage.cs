using JustSaying.Models;

namespace JustSaying.Benchmark;

public class BenchmarkMessage(TimeSpan sentAtOffset, int sequenceId) : Message
{
    public TimeSpan SentAtOffset { get; } = sentAtOffset;
    public int SequenceId { get; } = sequenceId;
}
