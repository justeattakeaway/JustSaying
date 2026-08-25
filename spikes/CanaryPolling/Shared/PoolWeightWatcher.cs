using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CanaryDemo.Shared;

/// <summary>
/// The pod's view of the rollout signal: a JSON file mapping pool name to weight,
/// e.g. <c>{"primary":1.0,"canary":0.33}</c>, re-read when its timestamp changes.
/// This stands in for however a signal reaches pods in your platform — a
/// ConfigMap-mounted file (which Kubernetes updates in place, no restarts) fits
/// this shape exactly, but an env-refreshed flag service works the same way.
/// Pods only ever read; they never talk to each other.
/// </summary>
public sealed class PoolWeightWatcher(string weightsFile, string poolName, ILogger<PoolWeightWatcher> logger)
{
    private DateTime _lastWriteUtc;
    private double _weight = 1.0;

    public double CurrentWeight
    {
        get
        {
            try
            {
                var writeUtc = File.GetLastWriteTimeUtc(weightsFile);
                if (writeUtc != _lastWriteUtc)
                {
                    var weights = JsonSerializer.Deserialize<Dictionary<string, double>>(File.ReadAllText(weightsFile));
                    if (weights is not null && weights.TryGetValue(poolName, out double weight))
                    {
                        double clamped = Math.Clamp(weight, 0d, 1d);
                        if (Math.Abs(clamped - _weight) > double.Epsilon)
                        {
                            logger.LogInformation("Pool '{Pool}' weight changed {Old:0.###} -> {New:0.###}", poolName, _weight, clamped);
                        }

                        _weight = clamped;
                    }

                    _lastWriteUtc = writeUtc;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                // Mid-write read race or partial file: keep the previous weight and retry
                // on the next read. Logged because a pod silently stuck on a stale weight
                // is exactly the failure you want to be able to see.
                logger.LogWarning(ex, "Could not read weights file '{File}'; keeping weight {Weight:0.###}", weightsFile, _weight);
            }

            return _weight;
        }
    }
}
