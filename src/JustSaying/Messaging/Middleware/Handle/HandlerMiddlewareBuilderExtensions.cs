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
        /// <summary>
        /// Adds a <see cref="HandlerInvocationMiddleware{T}"/> to the current pipeline.
        /// </summary>
        /// <param name="builder">The current <see cref="HandlerMiddlewareBuilder"/>.</param>
        /// <param name="handler">A factory that creates <see cref="IHandlerAsync{T}"/> instances from
        /// a <see cref="HandlerResolutionContext"/>.</param>
        /// <typeparam name="TMessage">The type of the message that should be handled</typeparam>
        /// <returns>The current <see cref="HandlerMiddlewareBuilder"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static HandlerMiddlewareBuilder UseHandler<TMessage>(
            this HandlerMiddlewareBuilder builder,
            Func<HandlerResolutionContext, IHandlerAsync<TMessage>> handler) where TMessage : Message
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return builder.Use(new HandlerInvocationMiddleware<TMessage>(handler));
        }
    }
}
