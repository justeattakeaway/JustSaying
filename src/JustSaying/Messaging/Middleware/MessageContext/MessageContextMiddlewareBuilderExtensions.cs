using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Middleware.MessageContext
{
    public static class MessageContextMiddlewareBuilderExtensions
    {
        public static HandlerMiddlewareBuilder UseMessageContextAccessor(this HandlerMiddlewareBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var contextAccessor = builder.ServiceResolver.ResolveService<IMessageContextAccessor>();

            return builder.Use(new MessageContextAccessorMiddleware(contextAccessor));
        }
    }
}
