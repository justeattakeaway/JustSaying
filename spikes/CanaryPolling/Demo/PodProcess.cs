using System.Collections.Concurrent;
using System.Diagnostics;

namespace Demo;

/// <summary>
/// Launches one SampleApp instance as a real OS process (pods must not share state)
/// and collects the cumulative handled-count it reports on stdout.
/// </summary>
public sealed class PodProcess : IDisposable
{
    private readonly Process _process;

    public string Name { get; }
    public string Pool { get; }
    public long HandledCount => Interlocked.Read(ref _handledCount);
    public long Casualties => Interlocked.Read(ref _casualties);
    public long Delayed => Interlocked.Read(ref _delayed);
    public long MaxLatencyMs => Interlocked.Read(ref _maxLatencyMs);
    private long _handledCount;
    private long _casualties;
    private long _delayed;
    private long _maxLatencyMs;

    private PodProcess(string name, string pool, Process process)
    {
        Name = name;
        Pool = pool;
        _process = process;
    }

    public static PodProcess Start(string name, string pool, string sampleAppDll, IDictionary<string, string> environment)
    {
        var psi = new ProcessStartInfo("dotnet", $"exec \"{sampleAppDll}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var (key, value) in environment)
        {
            psi.Environment[key] = value;
        }

        psi.Environment["POD_NAME"] = name;
        psi.Environment["POOL_NAME"] = pool;

        var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start pod {name}.");
        var pod = new PodProcess(name, pool, process);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null && Environment.GetEnvironmentVariable("DEMO_DEBUG") is not null)
            {
                Console.WriteLine($"  [{name}:out] {e.Data}");
            }

            // "STAT <pool> <pod> <count> <casualties> <delayed> <maxLatencyMs>" (cumulative)
            if (e.Data is not null && e.Data.StartsWith("STAT ", StringComparison.Ordinal))
            {
                var parts = e.Data.Split(' ');
                if (parts.Length >= 7 && long.TryParse(parts[3], out long count))
                {
                    Interlocked.Exchange(ref pod._handledCount, count);
                    if (long.TryParse(parts[4], out long casualties)) Interlocked.Exchange(ref pod._casualties, casualties);
                    if (long.TryParse(parts[5], out long delayed)) Interlocked.Exchange(ref pod._delayed, delayed);
                    if (long.TryParse(parts[6], out long maxMs)) Interlocked.Exchange(ref pod._maxLatencyMs, maxMs);
                    Volatile.Write(ref pod._reported, true);
                }
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.Error.WriteLine($"  [{name}] {e.Data}");
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return pod;
    }

    /// <summary>True once the pod's stats loop is up, i.e. the app started successfully.</summary>
    public bool HasReported => Volatile.Read(ref _reported);
    private bool _reported;

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }

        _process.Dispose();
    }
}
