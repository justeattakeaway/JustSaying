using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.Middleware.ErrorHandling
{
    public static class ErrorHandlerMiddlewareBuilderExtensions
    {
        /// <summary>
        /// Adds an error handler to the pipeline that will call methods on the  the <see cref="IMessageMonitor"/>
        /// registered in services.
        /// </summary>
        /// <param name="builder">The <see cref="HandlerMiddlewareBuilder"/> to add the middleware to.</param>
        /// <returns>The current <see cref="HandlerMiddlewareBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">When the <see cref="HandlerMiddlewareBuilder"/> is null.</exception>
        public static HandlerMiddlewareBuilder UseErrorHandler(this HandlerMiddlewareBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            IMessageMonitor monitor = builder.ServiceResolver.ResolveService<IMessageMonitor>();

            return builder.Use(new ErrorHandlerMiddleware(monitor));
        }
    }
}
