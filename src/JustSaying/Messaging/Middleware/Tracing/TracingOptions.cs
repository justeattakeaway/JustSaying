namespace JustSaying.Messaging.Middleware.Tracing;

/// <summary>
/// Options for configuring distributed trace propagation behavior.
/// </summary>
[Obsolete("TracingOptions is deprecated. Link mode is the default and follows OTel semantic conventions for messaging. This type will be removed in a future version.")]
public class TracingOptions
{
    /// <summary>
    /// When true, the consumer span becomes a child of the producer span (same trace ID).
    /// When false (default), the consumer creates a new trace with a link to the producer span.
    /// </summary>
    public bool UseParentSpan { get; set; }
}
