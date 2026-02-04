using JustSaying.Fluent;
using JustSaying.Messaging.Middleware;

namespace JustSaying.Sample.ServiceDefaults.Tracing;

/// <summary>
/// Extension methods for adding distributed tracing to JustSaying.
/// </summary>
public static class JustSayingTracingExtensions
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
