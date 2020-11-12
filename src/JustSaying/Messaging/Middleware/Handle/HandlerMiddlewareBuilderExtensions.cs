using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.ExactlyOnce;
using JustSaying.Messaging.Middleware.Metrics;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.Handle.HandleMessageContext, bool>;

namespace JustSaying.Messaging.Middleware.Handle
{
    public static class HandlerMiddlewareBuilderExtensions
    {
        public static HandlerMiddlewareBuilder UseHandler<TMessage>(this HandlerMiddlewareBuilder builder,
            Func<HandlerResolutionContext, IHandlerAsync<TMessage>> handler) where TMessage : Message
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return builder.Use(new HandlerInvocationMiddleware<TMessage>(handler));
        }

        public static HandlerMiddlewareBuilder UseStopwatch(this HandlerMiddlewareBuilder builder,
            Type handlerType)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            IMessageMonitor monitor = builder.ServiceResolver.ResolveService<IMessageMonitor>();

            return builder.Use(new StopwatchMiddleware(monitor, handlerType));
        }

        public static HandlerMiddlewareBuilder UseExactlyOnce<TMessage>(
            this HandlerMiddlewareBuilder builder,
            string lockKey,
            TimeSpan? lockDuration = null)
        {
            HandleMessageMiddleware CreateMiddleware()
            {
                var messageLock = builder.ServiceResolver.ResolveService<IMessageLockAsync>();
                var logger = builder.ServiceResolver.ResolveService<ILogger<ExactlyOnceMiddleware<TMessage>>>();

                return new ExactlyOnceMiddleware<TMessage>(messageLock,
                    lockDuration ?? TimeSpan.MaxValue,
                    lockKey,
                    logger);
            }

            builder.Use(CreateMiddleware);

            return builder;
        }
    }
}
