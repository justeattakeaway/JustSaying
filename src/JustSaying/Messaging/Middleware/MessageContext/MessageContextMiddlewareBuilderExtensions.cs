using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware.Backoff;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

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
