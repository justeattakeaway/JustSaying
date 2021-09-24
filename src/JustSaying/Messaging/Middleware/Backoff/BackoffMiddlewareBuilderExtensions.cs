using System;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware.ErrorHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.Backoff
{
    public static class BackoffMiddlewareBuilderExtensions
    {
        /// <summary>
        /// If an <see cref="IMessageBackoffStrategy"/> has been registered in services, then this will create
        /// a <see cref="BackoffMiddleware"/> and add it to the pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="HandlerMiddlewareBuilder"/> to add the middleware to.</param>
        /// <param name="backoffStrategy">The <see cref="IMessageBackoffStrategy"/> to use to determine message visibility timeouts.</param>
        /// <returns>The current <see cref="HandlerMiddlewareBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">When the <see cref="HandlerMiddlewareBuilder"/> is null.</exception>
        public static HandlerMiddlewareBuilder UseBackoff(this HandlerMiddlewareBuilder builder, IMessageBackoffStrategy backoffStrategy)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var loggerFactory = builder.ServiceResolver.ResolveService<ILoggerFactory>();
            var monitor = builder.ServiceResolver.ResolveService<IMessageMonitor>();

            return builder.Use(new BackoffMiddleware(backoffStrategy, loggerFactory, monitor));
        }
    }
}
