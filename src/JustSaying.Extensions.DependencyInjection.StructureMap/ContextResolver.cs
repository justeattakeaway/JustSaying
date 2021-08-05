using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying
{
    /// <summary>
    /// A class that implements <see cref="IServiceResolver"/> and <see cref="IHandlerResolver"/>
    /// for <see cref="IContext"/>. This class cannot be inherited.
    /// </summary>
    internal sealed class ContextResolver : IServiceResolver, IHandlerResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextResolver"/> class.
        /// </summary>
        /// <param name="container">The <see cref="IContext"/> to use.</param>
        internal ContextResolver(IContext container)
        {
            Context = container;
        }

        /// <summary>
        /// Gets the <see cref="IContext"/> to use.
        /// </summary>
        private IContext Context { get; }

        /// <inheritdoc />
        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
            => Context.GetInstance<IHandlerAsync<T>>();

        /// <inheritdoc />
        public T ResolveService<T>() where T : class
            => Context.GetInstance<T>();

        public T ResolveOptionalService<T>() where T : class
            => Context.TryGetInstance<T>();
    }
}
