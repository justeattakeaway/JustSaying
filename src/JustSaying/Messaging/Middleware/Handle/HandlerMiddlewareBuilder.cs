using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware
{

    /// <summary>
    /// A class representing a builder for a middleware pipeline.
    /// </summary>
    public sealed class HandlerMiddlewareBuilder
    {
        private Action<HandlerMiddlewareBuilder> _configure;
        internal IServiceResolver ServiceResolver { get; }
        private IHandlerResolver HandlerResolver { get; }

        private readonly List<Func<HandleMessageMiddleware>> _middlewares;
        private HandleMessageMiddleware _handlerMiddleware;

        /// <summary>
        /// Creates a HandlerMiddlewareBuilder instance.
        /// </summary>
        /// <param name="handlerResolver">An <see cref="IHandlerResolver"/> that can create handlers.</param>
        /// <param name="serviceResolver">An <see cref="IServiceResolver"/> that enables resolution of middlewares
        /// and middleware services.</param>
        public HandlerMiddlewareBuilder(IHandlerResolver handlerResolver, IServiceResolver serviceResolver)
        {
            ServiceResolver = serviceResolver;
            HandlerResolver = handlerResolver;
            _middlewares = new List<Func<HandleMessageMiddleware>>();
        }

        /// <summary>
        /// Adds a middleware of type <typeparamref name="TMiddleware"/> to the pipeline which will be resolved from the
        /// <see cref="IServiceResolver"/>. It will be resolved once when the pipeline is built, and cached
        /// for the lifetime of the bus.
        /// </summary>
        /// <typeparam name="TMiddleware">The type of the middleware to add.</typeparam>
        /// <returns>The current HandlerMiddlewareBuilder.</returns>
        public HandlerMiddlewareBuilder Use<TMiddleware>() where TMiddleware : MiddlewareBase<HandleMessageContext, bool>
        {
            _middlewares.Add(() => ServiceResolver.ResolveService<TMiddleware>());
            return this;
        }

        /// <summary>
        /// Adds the provided middleware instance to the pipeline.
        /// </summary>
        /// <param name="middleware">An instance of a middleware to add to the pipeline.</param>
        /// <returns>The current HandlerMiddlewareBuilder.</returns>
        public HandlerMiddlewareBuilder Use(HandleMessageMiddleware middleware)
        {
            if (middleware == null) throw new ArgumentNullException(nameof(middleware));

            _middlewares.Add(() => middleware);
            return this;
        }


        /// <summary>
        /// Adds a middleware to the pipeline. The Func&lt;HandleMessageMiddleware&gt; will be called once
        /// when the pipeline is built and cached for the lifetime of the bus.
        /// </summary>
        /// <param name="middlewareFactory">A <see cref="Func{HandleMessageMiddleware}"/> that produces an
        /// instance of a middleware to use in the pipeline.</param>
        /// <returns>The current HandlerMiddlewareBuilder.</returns>
        public HandlerMiddlewareBuilder Use(Func<HandleMessageMiddleware> middlewareFactory)
        {
            if (middlewareFactory == null) throw new ArgumentNullException(nameof(middlewareFactory));

            _middlewares.Add(middlewareFactory);
            return this;
        }

        /// <summary>
        /// Adds a HandlerInvocationMiddleware{TMessage} to the pipeline. An <see cref="IHandlerAsync{TMessage}"/>
        /// will be resolved from JustSaying's <see cref="IHandlerResolver"/> for each message and invoked.
        ///
        /// This is added automatically as the innermost handler to all pipelines, and doesn't need
        /// to be called manually.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns>The current HandlerMiddlewareBuilder.</returns>
        /// <exception cref="InvalidOperationException">
        /// If a HandlerInvocationMiddleware already exists in this pipeline, it cannot be added again.
        /// </exception>
        public HandlerMiddlewareBuilder UseHandler<TMessage>() where TMessage : Message
        {
            if (_handlerMiddleware != null)
            {
                throw new InvalidOperationException(
                    $"Handler middleware has already been specified for {typeof(TMessage).Name} on this queue.");
            }

            _handlerMiddleware = new HandlerInvocationMiddleware<TMessage>(HandlerResolver.ResolveHandler<TMessage>);

            return this;
        }

        /// <summary>
        /// Provides a mechanism to delegate configuration of this pipeline to user code by passing around
        /// a configuration action. The provided action is invoked after the default middlewares are added,
        /// so that additional middlewares wrap the defaults.
        /// </summary>
        /// <param name="configure">An <see cref="Action{HandlerMiddlewareBuilder}"/> that customises
        /// the pipeline.</param>
        /// <returns></returns>

        public HandlerMiddlewareBuilder Configure(
            Action<HandlerMiddlewareBuilder> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        /// <summary>
        /// Produces a callable middleware chain from the configured middlewares.
        ///
        /// </summary>
        /// <returns>A callable <see cref="HandleMessageMiddleware"/></returns>
        public HandleMessageMiddleware Build()
        {
            _configure?.Invoke(this);

            // We reverse the middleware array so that the declaration order matches the execution order
            // (i.e. russian doll).
            var middlewares =
                _middlewares
                    .Select(m => m())
                    .Reverse()
                    .ToList();

            if (_handlerMiddleware != null)
            {
                // Handler middleware needs to be last in the chain, so we keep an explicit reference to
                // it and add it here
                middlewares.Insert(0, _handlerMiddleware);
            }

            return MiddlewareBuilder.BuildAsync(middlewares.ToArray());
        }
    }
}
