namespace JustSaying.AwsTools;

/// <summary>
/// Contains constant key values for message attributes.
/// </summary>
internal static class MessageAttributeKeys
{
    /// <summary>
    /// Represents the key for the Content-Encoding attribute.
    /// </summary>
    public const string ContentEncoding = "Content-Encoding";

    /// <summary>
    /// Represents the key for the W3C traceparent attribute used for distributed tracing.
    /// </summary>
    public const string TraceParent = "traceparent";

    /// <summary>
    /// Represents the key for the W3C tracestate attribute used for distributed tracing.
    /// </summary>
    public const string TraceState = "tracestate";
}
