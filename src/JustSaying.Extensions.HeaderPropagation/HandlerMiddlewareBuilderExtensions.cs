using JustSaying.Extensions.HeaderPropagation;

namespace JustSaying.Messaging.Middleware;

/// <summary>
/// Extension methods for configuring header propagation on a <see cref="HandlerMiddlewareBuilder"/>.
/// </summary>
public static class HandlerMiddlewareBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="HeaderValueFilterMiddleware"/> to the pipeline that filters messages based on
    /// a message attribute value. Messages that do not match are acknowledged without being processed.
    /// </summary>
    /// <param name="builder">The <see cref="HandlerMiddlewareBuilder"/> to configure.</param>
    /// <param name="headerName">The message attribute name to filter on.</param>
    /// <param name="expectedValue">
    /// The expected value. Pass <see langword="null"/> to route only default/production traffic
    /// (messages where the attribute is absent). Pass a non-null value to route only messages
    /// where the attribute equals that value.
    /// </param>
    /// <returns>The <see cref="HandlerMiddlewareBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static HandlerMiddlewareBuilder UseHeaderFilter(
        this HandlerMiddlewareBuilder builder,
        string headerName,
        string? expectedValue = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        return builder.Use(new HeaderValueFilterMiddleware(headerName, expectedValue));
    }
}
