using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.Fluent;
using HandleMessageMiddleware =
    JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.Handle.HandleMessageContext
        , bool>;

namespace JustSaying.Messaging.Middleware.Handle
{
    public static class HandlerMiddlewareBuilderExtensions
    {
        public static HandleMiddlewareBuilder UseHandlerMiddleware<TMessage>(
            this HandleMiddlewareBuilder builder,
            IHandlerResolver handlerResolver)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (handlerResolver == null) throw new ArgumentNullException(nameof(handlerResolver));

            builder.Use((services, next) =>
                new HandlerInvocationMiddleware<TMessage>(handlerResolver.ResolveHandler<TMessage>));

            return builder;
        }
    }

    /// <summary>
    /// Helper methods to chain instances of <see cref="MiddlewareBase{TContext, TOut}" />.
    /// </summary>
    public class HandleMiddlewareBuilder
    {
        private Action<HandleMiddlewareBuilder> _configure;
        private readonly IHandlerResolver _handlerResolver;
        private readonly IServiceResolver _serviceResolver;

        private readonly List<Func<IServiceResolver, HandleMessageMiddleware, HandleMessageMiddleware>>
            _middlewares;

        public HandleMiddlewareBuilder Use(
            Func<IServiceResolver, HandleMessageMiddleware, HandleMessageMiddleware> middleware)
        {
            _middlewares.Add(middleware);
            return this;
        }

        public HandleMiddlewareBuilder(IHandlerResolver handlerResolver, IServiceResolver serviceResolver)
        {
            _handlerResolver = handlerResolver;
            _serviceResolver = serviceResolver;
            _middlewares =
                new List<Func<IServiceResolver, HandleMessageMiddleware, HandleMessageMiddleware>>();
        }

        public HandleMiddlewareBuilder Configure(
            Action<HandleMiddlewareBuilder> configure)
        {
            _configure = configure;
            return this;
        }

        internal HandleMessageMiddleware Build<T>()
        {
            if (_configure == null)
            {
                this.UseHandlerMiddleware<T>(_handlerResolver);
            }
            else
            {
                _configure(this);
            }

            // Partially apply the service resolver
            var middlewares = _middlewares
                .Select<Func<IServiceResolver, HandleMessageMiddleware, HandleMessageMiddleware>,
                    Func<HandleMessageMiddleware, HandleMessageMiddleware>>(
                    m => next => m(_serviceResolver, next));

            return MiddlewareBuilder.BuildAsync(middlewares.ToArray());
        }
    }
}
