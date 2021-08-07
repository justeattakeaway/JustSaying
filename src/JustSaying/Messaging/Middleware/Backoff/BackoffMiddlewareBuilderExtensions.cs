using System;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware.ErrorHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.Backoff
{
    public static class BackoffMiddlewareBuilderExtensions
    {
        public static HandlerMiddlewareBuilder UseBackoff(this HandlerMiddlewareBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var backoffStrategy = builder.ServiceResolver.ResolveOptionalService<IMessageBackoffStrategy>();
            if (backoffStrategy == null) return builder;

            var loggerFactory = builder.ServiceResolver.ResolveService<ILoggerFactory>();
            var monitor = builder.ServiceResolver.ResolveService<IMessageMonitor>();

            return builder.Use(new BackoffMiddleware(backoffStrategy, loggerFactory, monitor));
        }
    }
}
