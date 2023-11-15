using System.Collections.Concurrent;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

internal class ServiceBuilderServiceResolver(ServicesBuilder builder) : IServiceResolver
{
    private readonly ConcurrentDictionary<Type, object> _serviceLookup = new();

    private bool _built = false;

    public T ResolveService<T>() where T : class
    {
        return ResolveOptionalService<T>() ??
               throw new InvalidOperationException(
                   $"Service type {typeof(T).FullName} isn't available from this service resolver.");
    }

    private void Build()
    {
        if (builder.HandlerResolver != null)
        {
            _serviceLookup[typeof(IHandlerResolver)] = builder.HandlerResolver();
        }

        if (builder.LoggerFactory != null)
        {
            _serviceLookup[typeof(ILoggerFactory)] = builder.LoggerFactory();
        }

        if (builder.BusBuilder.ClientFactoryBuilder != null)
        {
            _serviceLookup[typeof(IAwsClientFactory)] = builder.BusBuilder.ClientFactoryBuilder.Build();
        }

        if (builder.MessageMonitoring != null)
        {
            _serviceLookup[typeof(IMessageMonitor)] = builder.MessageMonitoring();
        }

        if (builder.SerializationRegister != null)
        {
            _serviceLookup[typeof(IMessageSerializationRegister)] = builder.SerializationRegister();
        }

        if (builder.MessageContextAccessor != null)
        {
            _serviceLookup[typeof(IMessageContextAccessor)] = builder.MessageContextAccessor();
        }

        _built = true;
    }

    public T ResolveOptionalService<T>() where T : class
    {
        if(!_built) Build();

        Type typeofT = typeof(T);
        if (_serviceLookup.TryGetValue(typeofT, out object result))
        {
            return (T)result;
        }

        return null;
    }
}
