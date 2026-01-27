using System.Diagnostics;
using System.Text;
using Confluent.Kafka;

namespace JustSaying.Extensions.Kafka.Tracing;

/// <summary>
/// Handles W3C Trace Context propagation for Kafka messages.
/// Injects and extracts trace context from Kafka headers.
/// </summary>
public static class TraceContextPropagator
{
    // W3C Trace Context header names
    public const string TraceParentHeader = "traceparent";
    public const string TraceStateHeader = "tracestate";

    /// <summary>
    /// Injects the current trace context into Kafka message headers.
    /// </summary>
    /// <param name="headers">The Kafka headers to inject into.</param>
    /// <param name="activity">The activity to propagate (uses Activity.Current if null).</param>
    public static void InjectTraceContext(Headers headers, Activity activity = null)
    {
        if (headers == null) return;

        activity ??= Activity.Current;
        if (activity == null) return;

        // Format: version-traceid-spanid-flags
        // Example: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
        var traceParent = FormatTraceParent(activity.TraceId, activity.SpanId, activity.ActivityTraceFlags);
        headers.Add(TraceParentHeader, Encoding.UTF8.GetBytes(traceParent));

        // Add tracestate if present
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers.Add(TraceStateHeader, Encoding.UTF8.GetBytes(activity.TraceStateString));
        }
    }

    /// <summary>
    /// Extracts trace context from Kafka message headers.
    /// </summary>
    /// <param name="headers">The Kafka headers to extract from.</param>
    /// <returns>The extracted ActivityContext, or null if not found.</returns>
    public static ActivityContext? ExtractTraceContext(Headers headers)
    {
        if (headers == null) return null;

        string traceParent = null;
        string traceState = null;

        foreach (var header in headers)
        {
            if (header.Key.Equals(TraceParentHeader, StringComparison.OrdinalIgnoreCase))
            {
                traceParent = Encoding.UTF8.GetString(header.GetValueBytes());
            }
            else if (header.Key.Equals(TraceStateHeader, StringComparison.OrdinalIgnoreCase))
            {
                traceState = Encoding.UTF8.GetString(header.GetValueBytes());
            }
        }

        return ParseTraceParent(traceParent, traceState);
    }

    /// <summary>
    /// Extracts trace context from a dictionary of headers.
    /// </summary>
    public static ActivityContext? ExtractTraceContext(IReadOnlyDictionary<string, string> headers)
    {
        if (headers == null) return null;

        headers.TryGetValue(TraceParentHeader, out var traceParent);
        headers.TryGetValue(TraceStateHeader, out var traceState);

        return ParseTraceParent(traceParent, traceState);
    }

    private static string FormatTraceParent(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags flags)
    {
        // W3C Trace Context format: version-traceid-spanid-flags
        return $"00-{traceId}-{spanId}-{(flags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00")}";
    }

    private static ActivityContext? ParseTraceParent(string traceParent, string traceState = null)
    {
        if (string.IsNullOrEmpty(traceParent)) return null;

        try
        {
            // Parse: version-traceid-spanid-flags
            var parts = traceParent.Split('-');
            if (parts.Length < 4) return null;

            var version = parts[0];
            if (version != "00") return null; // Only support version 00

            var traceIdStr = parts[1];
            var spanIdStr = parts[2];
            var flagsStr = parts[3];

            // Validate lengths (trace ID = 32 hex chars, span ID = 16 hex chars)
            if (traceIdStr.Length != 32 || spanIdStr.Length != 16)
            {
                return null;
            }

            var traceId = ActivityTraceId.CreateFromString(traceIdStr.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(spanIdStr.AsSpan());

            var flags = flagsStr == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;

            return new ActivityContext(traceId, spanId, flags, traceState);
        }
        catch
        {
            return null;
        }
    }
}
