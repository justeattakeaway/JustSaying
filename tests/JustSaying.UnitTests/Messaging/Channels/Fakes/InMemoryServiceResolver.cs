using System;
using System.Threading;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.Backoff;
using JustSaying.Messaging.Middleware.ErrorHandling;
using JustSaying.Messaging.Middleware.Logging;
using JustSaying.Messaging.Middleware.MessageContext;
using JustSaying.Messaging.Middleware.PostProcessing;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.Fakes
{
    public class InMemoryServiceResolver : IServiceResolver, IHandlerResolver
    {
        private readonly IServiceProvider _provider;

        private static readonly Action<IServiceCollection, ITestOutputHelper, IMessageMonitor> Configure = (sc, outputHelper, monitor) =>
            sc.AddLogging(l => l.AddXUnit(outputHelper))
            .AddSingleton<IMessageMonitor>(monitor)
            .AddSingleton<LoggingMiddleware>()
            .AddSingleton<SqsPostProcessorMiddleware>()
            .AddSingleton<IMessageContextAccessor>(new MessageContextAccessor());

        public InMemoryServiceResolver(ITestOutputHelper outputHelper, IMessageMonitor monitor, Action<IServiceCollection> configure = null) :
            this(sc =>
            {
                Configure(sc, outputHelper, monitor);
                configure?.Invoke(sc);
            })
        { }

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

        public T ResolveService<T>() where T : class
        {
            return _provider.GetRequiredService<T>();
        }

        public T ResolveOptionalService<T>() where T : class
        {
            return _provider.GetService<T>();
        }
    }
}
