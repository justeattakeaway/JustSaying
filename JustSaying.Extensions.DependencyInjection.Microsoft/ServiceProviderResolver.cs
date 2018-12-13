using System;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying
{
    /// <summary>
    /// A class that implements <see cref="IServiceResolver"/> and <see cref="IHandlerResolver"/>
    /// for <see cref="IServiceProvider"/>. This class cannot be inherited.
    /// </summary>
    internal sealed class ServiceProviderResolver : IServiceResolver, IHandlerResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderResolver"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use.</param>
        internal ServiceProviderResolver(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> to use.
        /// </summary>
        private IServiceProvider ServiceProvider { get; }

        /// <inheritdoc />
        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
            => ServiceProvider.GetRequiredService<IHandlerAsync<T>>();

        /// <inheritdoc />
        public T ResolveService<T>()
            => ServiceProvider.GetRequiredService<T>();
    }
}
