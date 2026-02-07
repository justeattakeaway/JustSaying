namespace JustSaying.Messaging.Middleware.Tracing;

/// <summary>
/// Extension methods for adding distributed tracing to JustSaying message handling.
/// </summary>
public static class TracingMiddlewareBuilderExtensions
{
    /// <summary>
    /// Adds distributed tracing middleware to JustSaying message handling.
    /// This middleware creates Activity spans for each message processed,
    /// with links back to the original publish span.
    /// </summary>
    public static HandlerMiddlewareBuilder UseTracingMiddleware(
        this HandlerMiddlewareBuilder builder)
    {
        return builder.Use<TracingMiddleware>();
    }
}
