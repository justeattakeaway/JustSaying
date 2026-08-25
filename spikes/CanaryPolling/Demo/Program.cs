using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon;
using Amazon.SQS;
using CanaryDemo.Shared;
using Demo;
using JustSaying;
using JustSaying.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Orchestrates the canary rollout demo. Everything in this project is demo/load
// machinery; the interesting code — the PWM polling throttle — lives in SampleApp.
//
// Topology: 2 "primary" + 2 "canary" SampleApp processes, one shared SQS queue.
// The rollout signal is a weights file the orchestrator writes and every pod watches:
//   {"primary": 1.0, "canary": 0.33}
//
// Modes:
//   dotnet run                     weight sweep on floci: canary 0.33 → 1.0 → 0.0 under steady load
//   dotnet run -- --regimes        fixed 80/20 target validated across traffic regimes:
//                                    backlog (queue backpressure, pods flat out)
//                                    steady  (continuous arrivals, queue near-empty)
//                                    idle    (sparse arrivals, all pods parked long-polling)
//   dotnet run -- --longpoll       casualty validation: steady + idle at the recommended
//                                    config, then churn (1s period stop/start) while
//                                    measuring end-to-end latency — the gate should
//                                    strand nothing ("casualties" = ~30s latency spikes)
//   dotnet run -- --aws            use real AWS SQS via the default credential chain
//                                  (queues are deleted afterwards); combine with a mode
//   dotnet run -- --quick          shorter phases (noisier numbers)
//   FLOCI_URL=http://...           use an already-running floci/SQS-compatible endpoint

bool useAws = args.Contains("--aws");
bool regimes = args.Contains("--regimes");
bool longPoll = args.Contains("--longpoll");
bool quick = args.Contains("--quick");

// --proxy: shape traffic in an SQS-aware proxy between the pods and the queue (the
// Istio/Envoy model) instead of inside the pods — pods run as completely vanilla
// consumers with zero canary configuration, pointed at a per-pool proxy listener.
// floci-only: an explicit (non-transparent) proxy in front of real AWS would need to
// re-sign requests, because SigV4 covers the Host header (see README).
bool proxyMode = args.Contains("--proxy");
if (proxyMode && useAws)
{
    Console.Error.WriteLine("--proxy is floci-only: an explicit proxy breaks SigV4 against real AWS (Host header is signed). In Istio the interception is transparent, so the signature survives.");
    return;
}

// 2 canary pods at this weight vs 2 primary pods at 1.0 targets a 20% canary share.
// The exact share the PWM model predicts differs slightly per regime (printed per
// scenario); owning that mapping is rollout tooling's job in real life.
const double CanaryWeight = 0.327;
const int SteadyMessagesPerSecond = 30;

const string ContainerName = "canary-demo-floci";
string flociUrl = null;
string accountId = null;
string region;

if (useAws)
{
    region = Environment.GetEnvironmentVariable("AWS_REGION")
        ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION")
        ?? (await RunProcessAsync("aws", "configure get region")).Trim();
    if (string.IsNullOrWhiteSpace(region))
    {
        throw new InvalidOperationException("Could not determine an AWS region (AWS_REGION / aws configure get region).");
    }

    // The SDK's default chain can't read every CLI auth source (e.g. `aws login`),
    // so borrow the CLI's short-lived credentials and put them in our environment;
    // the SDK finds them there, and spawned pods inherit them.
    if (Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") is null)
    {
        var creds = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            await RunProcessAsync("aws", "configure export-credentials"));
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", creds["AccessKeyId"].GetString());
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", creds["SecretAccessKey"].GetString());
        if (creds.TryGetValue("SessionToken", out var token) && token.ValueKind == JsonValueKind.String)
        {
            Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", token.GetString());
        }
    }
}
else
{
    flociUrl = Environment.GetEnvironmentVariable("FLOCI_URL") ?? "http://localhost:4599";
    accountId = RandomNumberGenerator.GetInt32(1_000_000_000).ToString("D9", CultureInfo.InvariantCulture) + "000";
    region = "eu-west-1";
}

string runId = Guid.NewGuid().ToString("N")[..8];
string queueName = $"canary-orders-{runId}";
string weightsFile = Path.Combine(Path.GetTempPath(), $"canary-weights-{runId}.json");

Console.WriteLine($"""
    Canary rollout demo — PWM via {(proxyMode ? "an SQS-aware proxy (the Istio model, pods unmodified)" : "the receive-middleware gate")} ({(longPoll ? "casualty validation" : regimes ? "traffic-regime validation" : "weight sweep")})
    Backend: {(useAws ? $"AWS SQS ({region})" : $"floci ({flociUrl})")}
    Queue: {queueName}
    Signal file: {weightsFile}

    """);

if (!useAws)
{
    await EnsureFlociAsync();
}

void WriteWeights(double canaryWeight) =>
    File.WriteAllText(weightsFile, JsonSerializer.Serialize(new Dictionary<string, double>
    {
        ["primary"] = 1.0,
        ["canary"] = canaryWeight,
    }));

// The producer service: an ordinary JustSaying publisher (this also creates the queue).
await using var producerServices = new ServiceCollection()
    .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Error))
    .AddJustSaying((config, _) =>
    {
        config.Messaging(m => m.WithRegion(region));
        if (!useAws)
        {
            config.Client(c => c.WithClientFactory(() => new FlociClientFactory(new Uri(flociUrl), accountId, region)));
        }

        config.Publications(p => p.WithQueue<CanaryOrder>(q => q.WithQueueName(queueName)));
    })
    .BuildServiceProvider();

var publisher = producerServices.GetRequiredService<IMessagePublisher>();
var batchPublisher = producerServices.GetRequiredService<IMessageBatchPublisher>();
await publisher.StartAsync(CancellationToken.None);

string sampleAppDll = LocateDll("SampleApp");

try
{
    if (proxyMode)
    {
        await RunProxySweepAsync();
    }
    else if (longPoll)
    {
        await RunLongPollAsync();
    }
    else if (regimes)
    {
        await RunRegimesAsync();
    }
    else
    {
        await RunSweepAsync();
    }
}
finally
{
    File.Delete(weightsFile);
    if (useAws)
    {
        await DeleteQueuesAsync();
    }
}

Console.WriteLine(useAws
    ? "Done."
    : $"Done. (The '{ContainerName}' container is left running for faster re-runs: docker rm -f {ContainerName})");

// ---------------------------------------------------------------------------
// Scenario sets
// ---------------------------------------------------------------------------

async Task RunSweepAsync()
{
    var phaseDuration = TimeSpan.FromSeconds(quick ? 15 : 30);
    (double Weight, double Target)[] phases = [(CanaryWeight, 0.20), (1.0, 0.50), (0.0, 0.00)];

    WriteWeights(phases[0].Weight);
    var pods = await StartPodsAsync(PodEnv(pwmPeriodSeconds: 2, receiveWaitSeconds: 1, handlerWorkMs: 5));
    try
    {
        foreach (var (weight, target) in phases)
        {
            WriteWeights(weight);
            Console.WriteLine($"Canary weight → {weight:0.00} (signal file updated; steady load {SteadyMessagesPerSecond} msg/s for {phaseDuration.TotalSeconds:0}s)");

            // Let pods pick up the file change and finish already-scheduled PWM pulses.
            await Task.Delay(TimeSpan.FromSeconds(4));

            var before = Snapshot(pods);
            await PublishSteadyAsync(SteadyMessagesPerSecond, phaseDuration);
            await Task.Delay(TimeSpan.FromSeconds(3)); // drain the tail

            Report(pods, before, $"weight {weight:0.00}", target);
            Console.WriteLine();
        }
    }
    finally
    {
        DisposePods(pods);
    }
}

async Task RunRegimesAsync()
{
    WriteWeights(CanaryWeight);

    // --- Regime 1: backlog / backpressure. The queue is pre-loaded and pods run
    // flat out, so processing capacity is the limit and the drain is elastic:
    // modeled canary share = w/(w+1). Slower handlers (40ms) stretch the drain
    // across many PWM periods (2s) so the duty cycle can average out.
    // Sized so the drain spans ~10 PWM periods; a 1-2 period drain is too lumpy to average out.
    int backlogCount = quick ? 2000 : 12_000;
    Console.WriteLine($"Regime 1: backlog — pre-loading {backlogCount} messages, then starting pods (PWM period 2s, handler 40ms)");
    await PublishBacklogAsync(backlogCount);

    var pods = await StartPodsAsync(PodEnv(pwmPeriodSeconds: 2, receiveWaitSeconds: 1, handlerWorkMs: 40));
    try
    {
        var before = Snapshot(pods); // zeros, but keeps reporting uniform
        var sw = Stopwatch.StartNew();
        await WaitUntilAsync(
            () => Task.FromResult(Total(pods) >= backlogCount),
            TimeSpan.FromMinutes(6),
            "backlog did not drain in time");
        Report(pods, before, $"backlog drained in {sw.Elapsed.TotalSeconds:0.0}s", Modeled: CanaryWeight / (CanaryWeight + 1));
        Console.WriteLine();
    }
    finally
    {
        DisposePods(pods);
    }

    // --- Regimes 2 + 3 share a pod set: PWM period 10s with a 1s receive wait.
    // The gate never cancels a poll, so the last poll of each on-window lingers up
    // to the wait time into the off-window. Keeping wait << period bounds that
    // distortion (and it only exists at all when the queue is idle).
    pods = await StartPodsAsync(PodEnv(pwmPeriodSeconds: 10, receiveWaitSeconds: 1, handlerWorkMs: 5));
    try
    {
        // Regime 2: steady arrivals, queue near-empty, pods mostly waiting.
        var steadyDuration = TimeSpan.FromSeconds(quick ? 30 : 60);
        Console.WriteLine($"Regime 2: steady — {SteadyMessagesPerSecond} msg/s for {steadyDuration.TotalSeconds:0}s (queue near-empty)");
        var before = Snapshot(pods);
        await PublishSteadyAsync(SteadyMessagesPerSecond, steadyDuration);
        await Task.Delay(TimeSpan.FromSeconds(3));
        Report(pods, before, "steady arrivals", Modeled: ShareWhenOpenFractionIs(CanaryWeight));
        Console.WriteLine();

        // Regime 3: sparse arrivals — one message every 2 seconds, so every pod is
        // parked in an empty long poll when each message lands. This is the regime
        // where the split hinges on how SQS picks among waiting receivers.
        var idleDuration = TimeSpan.FromSeconds(quick ? 120 : 240);
        Console.WriteLine($"Regime 3: idle — 1 msg every 2s for {idleDuration.TotalSeconds:0}s (all pods parked long-polling)");
        before = Snapshot(pods);
        await PublishSparseAsync(TimeSpan.FromSeconds(2), idleDuration, pods, before);
        await Task.Delay(TimeSpan.FromSeconds(3));
        Report(pods, before, "idle / sparse arrivals", Modeled: ShareWhenOpenFractionIs(CanaryWeight + 1d / 10d));
        Console.WriteLine();
    }
    finally
    {
        DisposePods(pods);
    }

    // Expected canary share when each canary pod has an open receive for fraction p
    // of the time and SQS picks uniformly among open receivers (2 canary, 2 primary):
    // E[k/(k+2)] for k ~ Binomial(2, p). In the idle regime p gains ~wait/period of
    // lingering receive per cycle.
    static double ShareWhenOpenFractionIs(double p) => (2 * p * (1 - p)) / 3 + (p * p) / 2;
}

// The Istio model: pods are completely vanilla consumers — the in-app gate simply does
// not exist for them (no weights file configured), and ALL the shaping happens in the
// SqsProxy sitting between them and the queue. Each pool hits its own proxy listener
// port — standing in for per-workload sidecar config — and only the proxy watches the
// weights file. Same weight sweep as the default demo, so results are comparable.
async Task RunProxySweepAsync()
{
    var phaseDuration = TimeSpan.FromSeconds(quick ? 15 : 30);
    (double Weight, double Target)[] phases = [(CanaryWeight, 0.20), (1.0, 0.50), (0.0, 0.00)];
    const int PrimaryPort = 4610;
    const int CanaryPort = 4611;

    WriteWeights(phases[0].Weight);

    // Start the proxy.
    var proxyPsi = new ProcessStartInfo("dotnet", $"exec \"{LocateDll("SqsProxy")}\"")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    proxyPsi.Environment["UPSTREAM"] = flociUrl;
    proxyPsi.Environment["PRIMARY_PORT"] = PrimaryPort.ToString(CultureInfo.InvariantCulture);
    proxyPsi.Environment["CANARY_PORT"] = CanaryPort.ToString(CultureInfo.InvariantCulture);
    proxyPsi.Environment["WEIGHTS_FILE"] = weightsFile;
    proxyPsi.Environment["PWM_PERIOD_SECONDS"] = "10";

    using var proxy = Process.Start(proxyPsi) ?? throw new InvalidOperationException("Failed to start SqsProxy.");
    proxy.ErrorDataReceived += (_, e) =>
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            Console.Error.WriteLine($"  [proxy] {e.Data}");
        }
    };
    proxy.BeginErrorReadLine();
    proxy.BeginOutputReadLine();

    var pods = new List<PodProcess>();
    try
    {
        using var health = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        await WaitUntilAsync(
            async () =>
            {
                try
                {
                    using var response = await health.GetAsync($"http://127.0.0.1:{PrimaryPort}/healthz");
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    return false;
                }
            },
            TimeSpan.FromSeconds(15),
            "SqsProxy did not become healthy");
        Console.WriteLine($"  proxy up: primary → :{PrimaryPort}, canary → :{CanaryPort}, upstream {flociUrl}");

        // Truly vanilla pods: no weights file, no PWM knobs — zero canary configuration.
        // The only pool-specific setting is which endpoint they talk to, standing in for
        // Istio's transparent per-workload interception.
        foreach (var (name, pool) in new[] { ("primary-1", "primary"), ("primary-2", "primary"), ("canary-1", "canary"), ("canary-2", "canary") })
        {
            var env = PodEnv(pwmPeriodSeconds: 10, receiveWaitSeconds: 1, handlerWorkMs: 5);
            env.Remove("WEIGHTS_FILE");
            env.Remove("PWM_PERIOD_SECONDS");
            env["SQS_ENDPOINT"] = $"http://127.0.0.1:{(pool == "canary" ? CanaryPort : PrimaryPort)}";
            pods.Add(PodProcess.Start(name, pool, sampleAppDll, env));
        }

        Console.WriteLine($"  started {pods.Count} vanilla pods (zero canary config), waiting for them to come up...");
        await WaitUntilAsync(
            () => Task.FromResult(pods.All(p => p.HasReported)),
            TimeSpan.FromSeconds(90),
            "pods did not start reporting");

        foreach (var (weight, target) in phases)
        {
            WriteWeights(weight);
            Console.WriteLine($"Canary weight → {weight:0.00} (signal read by the PROXY; steady load {SteadyMessagesPerSecond} msg/s for {phaseDuration.TotalSeconds:0}s)");

            await Task.Delay(TimeSpan.FromSeconds(4));
            var before = Snapshot(pods);
            await PublishSteadyAsync(SteadyMessagesPerSecond, phaseDuration);
            await Task.Delay(TimeSpan.FromSeconds(3));

            Report(pods, before, $"weight {weight:0.00}", target);
            Console.WriteLine();
        }

        ReportLatency(pods);
    }
    finally
    {
        DisposePods(pods);
        try
        {
            if (!proxy.HasExited)
            {
                proxy.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}

// Validates that the gate throttle never strands messages: casualties (a message caught
// in an aborted delivery goes invisible until the ~30s visibility timeout) show up as
// handled messages with ~30s end-to-end latency, and the gate should produce none.
async Task RunLongPollAsync()
{
    WriteWeights(CanaryWeight);
    double modeled = (2 * CanaryWeight * (1 - CanaryWeight)) / 3 + (CanaryWeight * CanaryWeight) / 2;

    // --- Part 1: the recommended config — 10s PWM period, 1s receive wait (the gate
    // never cancels a poll, so the wait must stay well under the period).
    const double Part1Wait = 1;
    Console.WriteLine($"Part 1: wait {Part1Wait:0}s, period 10s");
    var pods = await StartPodsAsync(PodEnv(pwmPeriodSeconds: 10, receiveWaitSeconds: Part1Wait, handlerWorkMs: 5));
    try
    {
        var steadyDuration = TimeSpan.FromSeconds(quick ? 30 : 60);
        Console.WriteLine($"  steady {SteadyMessagesPerSecond} msg/s for {steadyDuration.TotalSeconds:0}s");
        var before = Snapshot(pods);
        await PublishSteadyAsync(SteadyMessagesPerSecond, steadyDuration);
        await Task.Delay(TimeSpan.FromSeconds(3));
        Report(pods, before, "steady", modeled);
        Console.WriteLine();

        var idleDuration = TimeSpan.FromSeconds(quick ? 120 : 240);
        Console.WriteLine($"  idle — 1 msg every 2s for {idleDuration.TotalSeconds:0}s (the regime that used to collapse)");
        before = Snapshot(pods);
        await PublishSparseAsync(TimeSpan.FromSeconds(2), idleDuration, pods, before);
        await Task.Delay(TimeSpan.FromSeconds(3));
        Report(pods, before, "idle", modeled);
        ReportLatency(pods);
        Console.WriteLine();
    }
    finally
    {
        DisposePods(pods);
    }

    // --- Part 2: churn. A 1s PWM period with 20s waits means each canary cancels an
    // in-flight receive roughly once a second, under load. The question: do messages
    // that were mid-delivery when the call was cancelled become "casualties" — received
    // (invisible) but unprocessed until the visibility timeout (30s) redelivers them?
    // Casualties show up as handled messages with ~30s end-to-end latency.
    var churnDuration = TimeSpan.FromSeconds(quick ? 60 : 120);
    Console.WriteLine($"Part 2: churn — period 1s, steady {SteadyMessagesPerSecond} msg/s for {churnDuration.TotalSeconds:0}s");
    pods = await StartPodsAsync(PodEnv(pwmPeriodSeconds: 1, receiveWaitSeconds: 1, handlerWorkMs: 5));
    try
    {
        var before = Snapshot(pods);
        int published = await PublishSteadyAsync(SteadyMessagesPerSecond, churnDuration);

        // Let stragglers arrive: a casualty takes ~30s (visibility timeout) to come back.
        Console.WriteLine($"  published {published}; waiting for the tail (casualties redeliver after ~30s)...");
        var sw = Stopwatch.StartNew();
        while (Total(pods) < published && sw.Elapsed < TimeSpan.FromSeconds(120))
        {
            await Task.Delay(1000);
        }

        Report(pods, before, $"churn ({Total(pods)}/{published} accounted for)", modeled);
        ReportLatency(pods);
        Console.WriteLine();
    }
    finally
    {
        DisposePods(pods);
    }
}

// ---------------------------------------------------------------------------
// Measurement + load helpers
// ---------------------------------------------------------------------------

static long Total(List<PodProcess> pods) => pods.Sum(p => p.HandledCount);

static (long Primary, long Canary) Snapshot(List<PodProcess> pods) => (
    pods.Where(p => p.Pool == "primary").Sum(p => p.HandledCount),
    pods.Where(p => p.Pool == "canary").Sum(p => p.HandledCount));

static void Report(List<PodProcess> pods, (long Primary, long Canary) before, string label, double Modeled)
{
    var after = Snapshot(pods);
    long primary = after.Primary - before.Primary;
    long canary = after.Canary - before.Canary;
    long total = primary + canary;
    double share = total == 0 ? 0 : canary / (double)total;
    Console.WriteLine($"  {label,-40} primary={primary,5}  canary={canary,5}  canary share={share,7:P1}  (modeled {Modeled:P1})");
    Console.WriteLine($"  per pod: {string.Join("  ", pods.Select(p => $"{p.Name}={p.HandledCount}"))}");
}

// Latency buckets are cumulative per pod set (pods are fresh per scenario part).
static void ReportLatency(List<PodProcess> pods)
{
    long total = pods.Sum(p => p.HandledCount);
    long casualties = pods.Sum(p => p.Casualties);
    long delayed = pods.Sum(p => p.Delayed);
    long maxMs = pods.Max(p => p.MaxLatencyMs);
    double rate = total == 0 ? 0 : casualties / (double)total;
    Console.WriteLine($"  latency: casualties (>15s, i.e. visibility-timeout redeliveries)={casualties} ({rate:P2} of {total}), delayed 5-15s={delayed}, max={maxMs / 1000.0:0.0}s");
}

async Task<int> PublishSteadyAsync(int perSecond, TimeSpan duration)
{
    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
    int perTick = Math.Max(1, perSecond / 10);
    int sequence = 0;
    var sw = Stopwatch.StartNew();
    while (sw.Elapsed < duration && await timer.WaitForNextTickAsync())
    {
        for (int i = 0; i < perTick; i++)
        {
            await publisher.PublishAsync(new CanaryOrder { Sequence = sequence++, PublishedAtUtc = DateTime.UtcNow });
        }
    }

    return sequence;
}

async Task PublishSparseAsync(TimeSpan gap, TimeSpan duration, List<PodProcess> pods, (long Primary, long Canary) before)
{
    using var timer = new PeriodicTimer(gap);
    int sequence = 0;
    var sw = Stopwatch.StartNew();
    var lastProgress = TimeSpan.Zero;
    while (sw.Elapsed < duration && await timer.WaitForNextTickAsync())
    {
        await publisher.PublishAsync(new CanaryOrder { Sequence = sequence++, PublishedAtUtc = DateTime.UtcNow });

        if (sw.Elapsed - lastProgress >= TimeSpan.FromSeconds(30))
        {
            lastProgress = sw.Elapsed;
            var now = Snapshot(pods);
            long p = now.Primary - before.Primary;
            long c = now.Canary - before.Canary;
            Console.WriteLine($"    t+{sw.Elapsed.TotalSeconds,3:0}s: {sequence} sent, primary={p} canary={c}");
        }
    }
}

async Task PublishBacklogAsync(int count)
{
    const int PerBatch = 10;   // one SQS SendMessageBatch per call
    const int Concurrency = 16;
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < count; i += PerBatch * Concurrency)
    {
        var calls = Enumerable.Range(0, Concurrency)
            .Select(c => i + c * PerBatch)
            .Where(start => start < count)
            .Select(start => batchPublisher.PublishAsync(
                Enumerable.Range(start, Math.Min(PerBatch, count - start))
                    .Select(seq => (JustSaying.Models.Message)new CanaryOrder { Sequence = seq, PublishedAtUtc = DateTime.UtcNow })
                    .ToList()));
        await Task.WhenAll(calls);
    }

    Console.WriteLine($"  published {count} messages in {sw.Elapsed.TotalSeconds:0.0}s");
}

// ---------------------------------------------------------------------------
// Pod + environment plumbing
// ---------------------------------------------------------------------------

Dictionary<string, string> PodEnv(double pwmPeriodSeconds, double receiveWaitSeconds, double handlerWorkMs)
{
    var env = new Dictionary<string, string>
    {
        ["AWS_REGION"] = region,
        ["QUEUE_NAME"] = queueName,
        ["WEIGHTS_FILE"] = weightsFile,
        ["PWM_PERIOD_SECONDS"] = pwmPeriodSeconds.ToString(CultureInfo.InvariantCulture),
        ["RECEIVE_WAIT_SECONDS"] = receiveWaitSeconds.ToString(CultureInfo.InvariantCulture),
        ["HANDLER_WORK_MS"] = handlerWorkMs.ToString(CultureInfo.InvariantCulture),
    };

    if (Environment.GetEnvironmentVariable("DEMO_POD_LOG_LEVEL") is { } podLogLevel)
    {
        env["LOG_LEVEL"] = podLogLevel;
    }

    if (!useAws)
    {
        env["SQS_ENDPOINT"] = flociUrl;
        env["AWS_ACCOUNT_ID"] = accountId;
    }

    return env;
}

async Task<List<PodProcess>> StartPodsAsync(Dictionary<string, string> environment)
{
    var pods = new List<PodProcess>();
    foreach (var (name, pool) in new[] { ("primary-1", "primary"), ("primary-2", "primary"), ("canary-1", "canary"), ("canary-2", "canary") })
    {
        pods.Add(PodProcess.Start(name, pool, sampleAppDll, environment));
    }

    Console.WriteLine($"  started {pods.Count} pods, waiting for them to come up...");
    try
    {
        await WaitUntilAsync(
            () => Task.FromResult(pods.All(p => p.HasReported)),
            TimeSpan.FromSeconds(90),
            "pods did not start reporting");
    }
    catch
    {
        DisposePods(pods);
        throw;
    }

    return pods;
}

static void DisposePods(List<PodProcess> pods)
{
    foreach (var pod in pods)
    {
        pod.Dispose();
    }
}

async Task EnsureFlociAsync()
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

    if (await IsReachableAsync())
    {
        return;
    }

    if (Environment.GetEnvironmentVariable("FLOCI_URL") is not null)
    {
        throw new InvalidOperationException($"FLOCI_URL={flociUrl} is not reachable.");
    }

    Console.WriteLine($"No SQS endpoint at {flociUrl}; starting container '{ContainerName}'...");
    await RunProcessAsync("docker", $"rm -f {ContainerName}", ignoreErrors: true);
    await RunProcessAsync("docker", $"run -d --name {ContainerName} -p 4599:4566 floci/floci:latest");
    await WaitUntilAsync(IsReachableAsync, TimeSpan.FromSeconds(30), $"floci did not become reachable at {flociUrl}");
    Console.WriteLine();

    async Task<bool> IsReachableAsync()
    {
        try
        {
            using var response = await http.GetAsync(flociUrl);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}

async Task DeleteQueuesAsync()
{
    try
    {
        using var sqs = new AmazonSQSClient(RegionEndpoint.GetBySystemName(region));
        var queues = await sqs.ListQueuesAsync(queueName); // prefix match catches the _error queue too
        foreach (var url in queues.QueueUrls ?? [])
        {
            if (url.EndsWith("_error", StringComparison.Ordinal))
            {
                var attrs = await sqs.GetQueueAttributesAsync(url, ["ApproximateNumberOfMessages"]);
                Console.WriteLine($"  error queue holds {attrs.ApproximateNumberOfMessages} message(s) — {(attrs.ApproximateNumberOfMessages == 0 ? "nothing dead-lettered" : "some messages were dead-lettered!")}");
            }

            await sqs.DeleteQueueAsync(url);
            Console.WriteLine($"  deleted {url}");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  queue cleanup failed ({ex.Message}); delete queues with prefix '{queueName}' manually.");
    }
}

static async Task<string> RunProcessAsync(string fileName, string arguments, bool ignoreErrors = false)
{
    var psi = new ProcessStartInfo(fileName, arguments)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to run {fileName}. Is it installed?");
    string stdout = await process.StandardOutput.ReadToEndAsync();
    string stderr = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    if (!ignoreErrors && process.ExitCode != 0)
    {
        throw new InvalidOperationException($"{fileName} {arguments} failed: {stderr.Trim()}");
    }

    return stdout;
}

static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, string timeoutMessage)
{
    var sw = Stopwatch.StartNew();
    while (sw.Elapsed < timeout)
    {
        if (await condition())
        {
            return;
        }

        await Task.Delay(500);
    }

    throw new TimeoutException(timeoutMessage);
}

string LocateDll(string projectName)
{
    // With the repo's UseArtifactsOutput layout, sibling project outputs live at
    // artifacts/bin/<Project>/<configuration>/. Resolve them relative to our own.
    string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    string configuration = Path.GetFileName(baseDir);
    string dll = Path.GetFullPath(Path.Combine(baseDir, "..", "..", projectName, configuration, $"{projectName}.dll"));
    if (!File.Exists(dll))
    {
        throw new FileNotFoundException($"{projectName}.dll not found at {dll}; build the {projectName} project first.");
    }

    return dll;
}
