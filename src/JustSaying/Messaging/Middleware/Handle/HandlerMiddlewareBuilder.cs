using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.Fluent;
using JustSaying.Models;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.Handle.HandleMessageContext, bool>;

namespace JustSaying.Messaging.Middleware.Handle
{
    /// <summary>
    /// Helper methods to chain instances of <see cref="MiddlewareBase{TContext, TOut}" />.
    /// </summary>
    public class HandlerMiddlewareBuilder
    {
        private Action<HandlerMiddlewareBuilder> _configure;

        internal IServiceResolver ServiceResolver { get; }
        internal IHandlerResolver HandlerResolver { get; }

        private readonly List<Func<HandleMessageMiddleware>> _middlewares;
        private HandleMessageMiddleware _handlerMiddleware;

        public HandlerMiddlewareBuilder(IHandlerResolver handlerResolver, IServiceResolver serviceResolver)
        {
            ServiceResolver = serviceResolver;
            HandlerResolver = handlerResolver;
            _middlewares = new List<Func<HandleMessageMiddleware>>();
        }

        public HandlerMiddlewareBuilder Use<TMiddleware>() where TMiddleware : MiddlewareBase<HandleMessageContext, bool>
        {
            _middlewares.Add(() => ServiceResolver.ResolveService<TMiddleware>());
            return this;
        }

        public HandlerMiddlewareBuilder Use(HandleMessageMiddleware middleware)
        {
            _middlewares.Add(() => middleware);
            return this;
        }

        public HandlerMiddlewareBuilder Use(Func<HandleMessageMiddleware> middlewareFactory)
        {
            _middlewares.Add(middlewareFactory);
            return this;
        }

        public HandlerMiddlewareBuilder UseHandler<TMessage>() where TMessage : Message
        {
            if (_handlerMiddleware != null)
            {
                throw new InvalidOperationException($"Handler middleware has already been specified " +
                    $"for {typeof(TMessage).Name} on this queue.");
            }

            _handlerMiddleware = new HandlerInvocationMiddleware<TMessage>(HandlerResolver.ResolveHandler<TMessage>);

            return this;
        }

        public HandlerMiddlewareBuilder Configure(
            Action<HandlerMiddlewareBuilder> configure)
        {
            _configure = configure;
            return this;
        }

        internal HandleMessageMiddleware Build()
        {
            _configure?.Invoke(this);

            if (_handlerMiddleware != null)
            {
                // Handler middleware needs to be last in the chain, so we keep an explicit reference to it and add it here
                _middlewares.Add(() => _handlerMiddleware);
            }

            var middlewares =
                _middlewares
                    .Select(m => m()).Reverse().ToArray();

            return MiddlewareBuilder.BuildAsync(middlewares);
        }
    }
}
