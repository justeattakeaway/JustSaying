using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    internal class ServiceBuilderServiceResolver : IServiceResolver
    {
        private readonly ServicesBuilder _builder;

        private readonly ConcurrentDictionary<Type, object> _serviceLookup = new();

        private bool _built = false;

        public ServiceBuilderServiceResolver(ServicesBuilder builder)
        {
            _builder = builder;
        }

        public T ResolveService<T>() where T : class
        {
            return ResolveOptionalService<T>() ??
                throw new InvalidOperationException(
                    $"Service type {typeof(T).FullName} isn't available from this service resolver.");
        }

        private void Build()
        {
            if (_builder.HandlerResolver != null)
            {
                _serviceLookup[typeof(IHandlerResolver)] = _builder.HandlerResolver();
            }

            if (_builder.LoggerFactory != null)
            {
                _serviceLookup[typeof(ILoggerFactory)] = _builder.LoggerFactory();
            }

            if (_builder.BusBuilder.ClientFactoryBuilder != null)
            {
                _serviceLookup[typeof(IAwsClientFactory)] = _builder.BusBuilder.ClientFactoryBuilder.Build();
            }

            if (_builder.MessageMonitoring != null)
            {
                _serviceLookup[typeof(IMessageMonitor)] = _builder.MessageMonitoring();
            }

            if (_builder.SerializationRegister != null)
            {
                _serviceLookup[typeof(IMessageSerializationRegister)] = _builder.SerializationRegister();
            }

            if (_builder.MessageContextAccessor != null)
            {
                _serviceLookup[typeof(IMessageContextAccessor)] = _builder.MessageContextAccessor();
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
}
