using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware.Backoff;
using JustSaying.Messaging.Middleware.ErrorHandling;
using JustSaying.Messaging.Middleware.MessageContext;
using JustSaying.Messaging.Middleware.PostProcessing;
using JustSaying.Models;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware
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
        /// <paramref name="builder"/> or <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        public static HandlerMiddlewareBuilder UseHandler<TMessage>(
            this HandlerMiddlewareBuilder builder,
            Func<HandlerResolutionContext, IHandlerAsync<TMessage>> handler) where TMessage : Message
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return builder.Use(new HandlerInvocationMiddleware<TMessage>(handler));
        }

        public static HandlerMiddlewareBuilder ApplyDefaults<TMessage>(
            this HandlerMiddlewareBuilder builder,
            Type handlerType,
            Action<HandlerMiddlewareBuilder> configure = null)
        where TMessage : Message
        {
            configure?.Invoke(builder);

            builder.UseMessageContextAccessor();
            builder.UseBackoff();
            builder.UseErrorHandler();
            builder.Use<LoggingMiddleware>();
            builder.UseStopwatch(handlerType);
            builder.Use<SqsPostProcessorMiddleware>();
            builder.UseHandler<TMessage>();

            return builder;
        }
    }
}
