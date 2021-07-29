using System;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.UnitTests.Messaging.Channels.Fakes
{
    public class InMemoryServiceResolver : IServiceResolver, IHandlerResolver
    {
        private readonly IServiceProvider _provider;

        public InMemoryServiceResolver(Action<IServiceCollection> configure = null)
        {
            var collection = new ServiceCollection();
            configure?.Invoke(collection);
            _provider = collection.BuildServiceProvider();
        }

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            return (IHandlerAsync<T>) _provider.GetService(typeof(IHandlerAsync<T>));
        }

        public T ResolveService<T>()
        {
            return _provider.GetService<T>();
        }
    }
}
