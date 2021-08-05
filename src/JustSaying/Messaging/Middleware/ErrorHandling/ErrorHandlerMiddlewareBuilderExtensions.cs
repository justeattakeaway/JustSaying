using System;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.Middleware.ErrorHandling
{
    public static class ErrorHandlerMiddlewareBuilderExtensions
    {
        public static HandlerMiddlewareBuilder UseErrorHandler(this HandlerMiddlewareBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            IMessageMonitor monitor = builder.ServiceResolver.ResolveService<IMessageMonitor>();

            return builder.Use(new ErrorHandlerMiddleware(monitor));
        }
    }
}
