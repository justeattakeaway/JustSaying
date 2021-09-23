using System;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware.Backoff;
using JustSaying.Messaging.Middleware.ErrorHandling;
using JustSaying.Messaging.Middleware.Logging;
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

        /// <summary>
        /// <para>
        /// Applies a set of default middlewares in order. Adding other middlewares before this will add them
        /// to the top of the stack, and are run first and last.
        /// Adding other middleware after this will add them to the bottom of the stack, just before the
        /// handler itself is invoked.
        /// </para>
        /// The middlewares this adds are, in order:
        /// <list type="bullet">
        /// <item>MessageContextAccessorMiddleware</item>
        /// <item>BackoffMiddleware (only if an <see cref="IMessageBackoffStrategy"/> is available)</item>
        /// <item>ErrorHandlerMiddleware</item>
        /// <item>LoggingMiddleware</item>
        /// <item>StopwatchMiddleware</item>
        /// <item>SqsPostProcessorMiddleware</item>
        /// <item>HandlerInvocationMiddleware`1</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The <see cref="HandlerMiddlewareBuilder"/> builder to add these defaults to.</param>
        /// <param name="handlerType">The type of the handler that will handle messages for this middleware pipeline.
        /// This is used when recording handler execution time with the StopwatchMiddleware.</param>
        /// <typeparam name="TMessage">The type of the message that this middleware pipeline handles.</typeparam>
        /// <returns>The current <see cref="HandlerMiddlewareBuilder"/>.</returns>
        public static HandlerMiddlewareBuilder UseDefaults<TMessage>(
            this HandlerMiddlewareBuilder builder,
            Type handlerType)
        where TMessage : Message
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (handlerType == null) throw new ArgumentNullException(nameof(handlerType), "HandlerType is used here to");

            builder.UseMessageContextAccessor();
            builder.UseErrorHandler();
            builder.Use<LoggingMiddleware>();
            builder.UseStopwatch(handlerType);
            builder.Use<SqsPostProcessorMiddleware>();
            builder.UseHandler<TMessage>();

            return builder;
        }
    }
}
