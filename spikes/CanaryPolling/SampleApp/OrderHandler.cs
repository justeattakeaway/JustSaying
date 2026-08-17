using CanaryDemo.Shared;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Hosting;

namespace SampleApp;

/// <summary>A stand-in for real message processing; counts what this pod handled.</summary>
public sealed class OrderHandler(HandledCounter counter, TimeSpan workTime) : IHandlerAsync<CanaryOrder>
{
    public async Task<bool> Handle(CanaryOrder message)
    {
        var latency = message.PublishedAtUtc == default
            ? TimeSpan.Zero
            : DateTime.UtcNow - message.PublishedAtUtc;
        counter.Record(latency);

        if (workTime > TimeSpan.Zero)
        {
            await Task.Delay(workTime); // simulated work
        }

        return true;
    }
}

/// <summary>
/// Counts handled messages and buckets their end-to-end latency. A message that was
/// mid-delivery when a receive call got cancelled isn't lost — it goes invisible until
/// the visibility timeout (30s by default) and is redelivered — so such "casualties"
/// show up here as handled messages with ~30s latency.
/// </summary>
public sealed class HandledCounter
{
    private static readonly TimeSpan DelayedThreshold = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CasualtyThreshold = TimeSpan.FromSeconds(15);

    private long _count;
    private long _delayed;
    private long _casualties;
    private long _maxLatencyMs;

    public void Record(TimeSpan latency)
    {
        Interlocked.Increment(ref _count);
        if (latency >= CasualtyThreshold)
        {
            Interlocked.Increment(ref _casualties);
        }
        else if (latency >= DelayedThreshold)
        {
            Interlocked.Increment(ref _delayed);
        }

        long ms = (long)latency.TotalMilliseconds;
        long seen;
        while (ms > (seen = Interlocked.Read(ref _maxLatencyMs)))
        {
            Interlocked.CompareExchange(ref _maxLatencyMs, ms, seen);
        }
    }

    public (long Count, long Casualties, long Delayed, long MaxLatencyMs) Snapshot() =>
        (Interlocked.Read(ref _count), Interlocked.Read(ref _casualties), Interlocked.Read(ref _delayed), Interlocked.Read(ref _maxLatencyMs));
}

/// <summary>
/// Demo-only: periodically writes machine-readable cumulative stats to stdout so the
/// orchestrator can measure the split. A real pod would emit metrics instead.
/// </summary>
public sealed class StatsReporter(HandledCounter counter, string poolName, string podName) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var (count, casualties, delayed, maxMs) = counter.Snapshot();
            Console.WriteLine($"STAT {poolName} {podName} {count} {casualties} {delayed} {maxMs}");
        }
    }
}
