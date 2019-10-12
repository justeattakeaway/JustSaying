using System;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing the built-in implementation of <see cref="IServiceResolver"/>. This class cannot be inherited.
    /// </summary>
    internal sealed class DefaultServiceResolver : IServiceResolver
    {
        /// <inheritdoc />
        public T ResolveService<T>()
            => (T)ResolveService(typeof(T));

        private object ResolveService(Type desiredType)
        {
            if (desiredType == typeof(ILoggerFactory))
            {
                return new NullLoggerFactory();
            }
            else if (desiredType == typeof(IAwsClientFactoryProxy))
            {
                return new AwsClientFactoryProxy();
            }
            else if (desiredType == typeof(IHandlerResolver))
            {
                return null; // Special case - must be provided by the consumer
            }
            else if (desiredType == typeof(IMessagingConfig))
            {
                return new MessagingConfig();
            }
            else if (desiredType == typeof(IMessageMonitor))
            {
                return new NullOpMessageMonitor();
            }
            else if (desiredType == typeof(IMessageSerializationFactory))
            {
                return new NewtonsoftSerializationFactory();
            }
            else if (desiredType == typeof(IMessageSerializationRegister))
            {
                return new MessageSerializationRegister(ResolveService<IMessageSubjectProvider>());
            }
            else if (desiredType == typeof(IMessageSubjectProvider))
            {
                return new NonGenericMessageSubjectProvider();
            }

            throw new NotSupportedException($"Resolving a service of type {desiredType.Name} is not supported.");
        }

        private sealed class NullLoggerFactory : ILoggerFactory
        {
            public void AddProvider(ILoggerProvider provider)
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new NullLogger();
            }

            public void Dispose()
            {
            }

            private sealed class NullLogger : ILogger
            {
                public IDisposable BeginScope<TState>(TState state)
                {
                    return null;
                }

                public bool IsEnabled(LogLevel logLevel)
                {
                    return false;
                }

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                }
            }
        }
    }
}
