using JustSaying.Fluent;
using PublishMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.PublishContext, bool>;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

/// <summary>
/// A builder for a publish middleware pipeline.
/// </summary>
/// <param name="serviceResolver">An <see cref="IServiceResolver"/> that enables resolution of middlewares
/// and middleware services.</param>
public sealed class PublishMiddlewareBuilder(IServiceResolver serviceResolver)
{
    private Action<PublishMiddlewareBuilder> _configure;
    internal IServiceResolver ServiceResolver { get; } = serviceResolver;

    private readonly List<Func<PublishMessageMiddleware>> _middlewares = [];

    /// <summary>
    /// Adds a middleware of type <typeparamref name="TMiddleware"/> to the pipeline which will be resolved from the
    /// <see cref="IServiceResolver"/>. It will be resolved once when the pipeline is built, and cached
    /// for the lifetime of the bus.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of the middleware to add.</typeparam>
    /// <returns>The current <see cref="PublishMiddlewareBuilder"/>.</returns>
    /// <exception cref="InvalidOperationException">When the middleware is not registered as Transient, an exception will be thrown if the resolved middleware is already part of a pipeline.</exception>
    public PublishMiddlewareBuilder Use<TMiddleware>() where TMiddleware : MiddlewareBase<PublishContext, bool>
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
    /// <returns>The current <see cref="PublishMiddlewareBuilder"/>.</returns>
    public PublishMiddlewareBuilder Use(PublishMessageMiddleware middleware)
    {
        if (middleware == null) throw new ArgumentNullException(nameof(middleware));

        _middlewares.Add(() => middleware);
        return this;
    }

    /// <summary>
    /// Adds a middleware to the pipeline. The Func will be called once
    /// when the pipeline is built and cached for the lifetime of the bus.
    /// </summary>
    /// <param name="middlewareFactory">A factory that produces an instance of a middleware to use in the pipeline.</param>
    /// <returns>The current <see cref="PublishMiddlewareBuilder"/>.</returns>
    public PublishMiddlewareBuilder Use(Func<PublishMessageMiddleware> middlewareFactory)
    {
        if (middlewareFactory == null) throw new ArgumentNullException(nameof(middlewareFactory));

        _middlewares.Add(middlewareFactory);
        return this;
    }

    /// <summary>
    /// Provides a mechanism to delegate configuration of this pipeline to user code by passing around
    /// a configuration action.
    /// </summary>
    /// <param name="configure">An action that customises the pipeline.</param>
    /// <returns>The current <see cref="PublishMiddlewareBuilder"/>.</returns>
    public PublishMiddlewareBuilder Configure(
        Action<PublishMiddlewareBuilder> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    /// <summary>
    /// Produces a callable middleware chain from the configured middlewares.
    /// </summary>
    /// <returns>A callable publish middleware chain.</returns>
    public PublishMessageMiddleware Build()
    {
        _configure?.Invoke(this);

        // We reverse the middleware array so that the declaration order matches the execution order
        // (i.e. russian doll).
        var middlewares =
            _middlewares
                .Select(m => m())
                .Reverse()
                .ToArray();

        return MiddlewareBuilder.BuildAsync(middlewares);
    }
}
