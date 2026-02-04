namespace JustSaying.Sample.ServiceDefaults.Tracing;

/// <summary>
/// Message attribute keys for distributed trace context propagation.
/// Uses W3C Trace Context standard names.
/// </summary>
public static class TraceContextKeys
{
    /// <summary>
    /// W3C traceparent header containing version, trace-id, parent-id, and trace-flags.
    /// Format: {version}-{trace-id}-{parent-id}-{trace-flags}
    /// Example: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
    /// </summary>
    public const string TraceParent = "traceparent";

    /// <summary>
    /// W3C tracestate header for vendor-specific trace information.
    /// </summary>
    public const string TraceState = "tracestate";

    /// <summary>
    /// Optional message identifier for correlation.
    /// </summary>
    public const string MessageId = "message_id";
}
