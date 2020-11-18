using System;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.Middleware
{
    public static class MetricsHandlerMiddlewareBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="StopwatchMiddleware"/> to the current pipeline.
        /// </summary>
        /// <param name="builder">The current <see cref="HandlerMiddlewareBuilder"/>.</param>
        /// <param name="handlerType">The type of the handler that results should be reported against.</param>
        /// <returns>The current <see cref="HandlerMiddlewareBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HandlerMiddlewareBuilder UseStopwatch(
            this HandlerMiddlewareBuilder builder,
            Type handlerType)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            IMessageMonitor monitor = builder.ServiceResolver.ResolveService<IMessageMonitor>();

            return builder.Use(new StopwatchMiddleware(monitor, handlerType));
        }
    }
}
