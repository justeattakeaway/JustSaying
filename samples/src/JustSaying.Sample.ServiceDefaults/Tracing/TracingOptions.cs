namespace JustSaying.Sample.ServiceDefaults.Tracing;

/// <summary>
/// Options for configuring distributed trace propagation behavior.
/// </summary>
public class TracingOptions
{
    /// <summary>
    /// When true, the consumer span becomes a child of the producer span,
    /// creating a single end-to-end trace. When false (default), the consumer
    /// creates a new trace with a link to the producer span.
    /// </summary>
    public bool UseParentSpan { get; set; }
}
