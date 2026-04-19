using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace JustSaying.Messaging.Monitoring;

/// <summary>
/// Provides the <see cref="ActivitySource"/> and <see cref="Meter"/> used by JustSaying
/// for OpenTelemetry-compatible distributed tracing and metrics.
/// </summary>
public static class JustSayingDiagnostics
{
    /// <summary>
    /// The name of the <see cref="ActivitySource"/> used by JustSaying.
    /// </summary>
    public const string ActivitySourceName = "JustSaying";

    /// <summary>
    /// The name of the <see cref="Meter"/> used by JustSaying.
    /// </summary>
    public const string MeterName = "JustSaying";

    private static readonly string Version =
        typeof(JustSayingDiagnostics).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0";

    /// <summary>
    /// Gets the <see cref="System.Diagnostics.ActivitySource"/> used by JustSaying for distributed tracing.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName, Version);

    /// <summary>
    /// Gets the <see cref="System.Diagnostics.Metrics.Meter"/> used by JustSaying for metrics.
    /// </summary>
    public static Meter Meter { get; } = new(MeterName, Version);

    internal static readonly Counter<long> ClientSentMessages =
        Meter.CreateCounter<long>("messaging.client.sent.messages", unit: "{message}", description: "Number of messages a producer attempted to send.");

    internal static readonly Histogram<double> ClientOperationDuration =
        Meter.CreateHistogram<double>("messaging.client.operation.duration", unit: "s", description: "Duration of messaging operation initiated by a producer or consumer client.");

    internal static readonly Counter<long> MessagesReceived =
        Meter.CreateCounter<long>("justsaying.messages.received", unit: "{message}", description: "Number of messages received from SQS.");

    internal static readonly Histogram<double> ProcessDuration =
        Meter.CreateHistogram<double>("messaging.process.duration", unit: "s", description: "Duration of processing operation.");

    internal static readonly Counter<long> MessagesProcessed =
        Meter.CreateCounter<long>("justsaying.messages.processed", unit: "{message}", description: "Number of messages processed by handlers.");

    internal static readonly Counter<long> MessagesThrottled =
        Meter.CreateCounter<long>("justsaying.messages.throttled", unit: "{event}", description: "Number of times message receiving was throttled.");
}
