using JustSaying.Models;

namespace JustSaying.Benchmark
{
    public class BenchmarkMessage : Message
    {
        public BenchmarkMessage(TimeSpan sentAtOffset, int sequenceId)
        {
            SentAtOffset = sentAtOffset;
            SequenceId = sequenceId;
        }

        public TimeSpan SentAtOffset { get; }
        public int SequenceId { get; }
    }
}
