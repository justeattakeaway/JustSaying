using System.Diagnostics;

namespace CanaryDemo.Shared;

/// <summary>
/// The PWM clock: on for <c>weight × period</c>, off for the rest, random phase per
/// instance. Weight comes from the watched rollout signal on every check, so changes
/// apply within 250ms; weight 1 never gates, weight 0 parks the caller (checked every
/// 250ms). Used by both the in-app receive-middleware gate and the SQS proxy.
/// </summary>
public sealed class PwmGate(PoolWeightWatcher weights, TimeSpan period)
{
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromMilliseconds(250);

    private readonly long _startTimestamp = Stopwatch.GetTimestamp();
    private readonly double _phaseOffsetMs = Random.Shared.NextDouble() * period.TotalMilliseconds;

    /// <summary>Waits until the on-window; only returns when the window is open.</summary>
    public async Task WaitForOnWindowAsync(CancellationToken cancellationToken) =>
        await TryWaitForOnWindowAsync(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Waits until the on-window or the timeout, whichever comes first. Returns true if
    /// the window is open; false if the timeout elapsed while still gated (the proxy
    /// uses this to emulate an empty long poll of the request's WaitTimeSeconds).
    /// </summary>
    public async Task<bool> TryWaitForOnWindowAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        long deadline = timeout == Timeout.InfiniteTimeSpan
            ? long.MaxValue
            : Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);

        while (true)
        {
            double weight = weights.CurrentWeight;

            if (weight >= 1d)
            {
                return true;
            }

            TimeSpan delay;
            if (weight > 0d)
            {
                double positionMs = (Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds + _phaseOffsetMs)
                    % period.TotalMilliseconds;
                if (positionMs < period.TotalMilliseconds * weight)
                {
                    return true;
                }

                double untilWindowOpensMs = period.TotalMilliseconds - positionMs;
                delay = TimeSpan.FromMilliseconds(Math.Min(untilWindowOpensMs, RecheckDelay.TotalMilliseconds));
            }
            else
            {
                delay = RecheckDelay; // parked; the weight might come back
            }

            long now = Stopwatch.GetTimestamp();
            if (now >= deadline)
            {
                return false;
            }

            var untilDeadline = Stopwatch.GetElapsedTime(now, deadline);
            await Task.Delay(delay < untilDeadline ? delay : untilDeadline, cancellationToken).ConfigureAwait(false);
        }
    }
}
