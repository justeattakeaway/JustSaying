using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.Logging;
using JustSaying.Models;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

/// <summary>
/// A class representing a builder for a middleware pipeline.
/// </summary>
/// <remarks>
/// Creates a HandlerMiddlewareBuilder instance.
/// </remarks>
/// <param name="handlerResolver">An <see cref="IHandlerResolver"/> that can create handlers.</param>
/// <param name="serviceResolver">An <see cref="IServiceResolver"/> that enables resolution of middlewares
/// and middleware services.</param>
public sealed class HandlerMiddlewareBuilder(IHandlerResolver handlerResolver, IServiceResolver serviceResolver)
{
    private Action<HandlerMiddlewareBuilder> _configure;
    internal IServiceResolver ServiceResolver { get; } = serviceResolver;

    /// <summary>
    /// Creates a builder for the handling pipeline of a subscription to <paramref name="messageType"/>.
    /// <see cref="Build"/> then requires the pipeline to invoke a handler, and middleware that is typed
    /// on the message can check that it matches. <paramref name="messageMetadataProvider"/> is the bus's
    /// provider, given to the <see cref="LoggingMiddleware"/> so handle logs read the same message
    /// identity as publish logs.
    /// </summary>
    internal HandlerMiddlewareBuilder(
        IHandlerResolver handlerResolver,
        IServiceResolver serviceResolver,
        Type messageType,
        IMessageMetadataProvider messageMetadataProvider)
        : this(handlerResolver, serviceResolver)
    {
        MessageType = messageType;
        _messageMetadataProvider = messageMetadataProvider;
    }

    private readonly IMessageMetadataProvider _messageMetadataProvider;

    /// <summary>
    /// Gets the type of message the pipeline handles, when it is built for a subscription.
    /// </summary>
    internal Type MessageType { get; }

    private readonly List<Func<HandleMessageMiddleware>> _middlewares = [];
    private HandleMessageMiddleware _handlerMiddleware;

    /// <summary>
    /// Adds a middleware of type <typeparamref name="TMiddleware"/> to the pipeline which will be resolved from the
    /// <see cref="IServiceResolver"/>. It will be resolved once when the pipeline is built, and cached
    /// for the lifetime of the bus.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of the middleware to add.</typeparam>
    /// <returns>The current HandlerMiddlewareBuilder.</returns>
    /// <exception cref="InvalidOperationException">When the middleware is not registered as Transient, an exception will be thrown if the resolved middleware is already part of a pipeline.</exception>
    public HandlerMiddlewareBuilder Use<TMiddleware>() where TMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        var newMiddleware = ServiceResolver.ResolveService<TMiddleware>();
        if (newMiddleware.HasNext)
        {
            throw new InvalidOperationException(
                @"Middlewares must be registered into your DI container such that each resolution creates a new instance.
For StructureMap use AlwaysUnique(), and for Microsoft.Extensions.DependencyInjection, use AddTransient().
Please check the documentation for your container for more details.");
        }

        _middlewares.Add(() => newMiddleware);
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
    public HandlerMiddlewareBuilder UseHandler<TMessage>()
    {
        if (_handlerMiddleware != null)
        {
            throw new InvalidOperationException(
                $"Handler middleware has already been specified for {typeof(TMessage).Name} on this queue.");
        }

        _handlerMiddleware = new HandlerInvocationMiddleware<TMessage>(handlerResolver.ResolveHandler<TMessage>);

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
    /// <exception cref="InvalidOperationException">
    /// The pipeline is for a subscription and never invokes a handler, for example because a middleware
    /// configuration replaced the defaults without calling <c>UseDefaults</c> or <c>UseHandler</c>.
    /// </exception>
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

        if (_messageMetadataProvider != null)
        {
            foreach (var loggingMiddleware in middlewares.OfType<LoggingMiddleware>())
            {
                loggingMiddleware.MetadataProvider = _messageMetadataProvider;
            }
        }

        if (MessageType != null && !middlewares.Any(IsHandlerInvocation))
        {
            throw new InvalidOperationException(
                $"The middleware pipeline for message type '{MessageType.FullName}' never invokes a handler, so its messages would never be handled. " +
                $"A middleware configuration replaces the default pipeline, so it must add the handler: call UseDefaults<{MessageType.Name}>(handlerType) " +
                $"(or UseHandler<{MessageType.Name}>()) in the configuration, after any middleware that should wrap the defaults.");
        }

        return MiddlewareBuilder.BuildAsync(middlewares.ToArray());
    }

    private static bool IsHandlerInvocation(HandleMessageMiddleware middleware)
    {
        var type = middleware.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HandlerInvocationMiddleware<>);
    }
}
