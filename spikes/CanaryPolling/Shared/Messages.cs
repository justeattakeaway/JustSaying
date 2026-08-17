using JustSaying.Models;

namespace CanaryDemo.Shared;

public sealed class CanaryOrder : Message
{
    public int Sequence { get; set; }

    /// <summary>Stamped by the producer so consumers can measure end-to-end latency.</summary>
    public DateTime PublishedAtUtc { get; set; }
}
