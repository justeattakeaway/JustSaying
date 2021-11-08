using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing the built-in implementation of <see cref="IServiceResolver"/>. This class cannot be inherited.
/// </summary>
internal sealed class DefaultServiceResolver : IServiceResolver
{
    /// <inheritdoc />
    public T ResolveService<T>() where T : class
        => (T)TryResolveService(typeof(T)) ??
           throw new NotSupportedException($"Resolving a service of type {typeof(T).Name} is not supported.");

    public T ResolveOptionalService<T>() where T : class
        => (T)TryResolveService(typeof(T));

    private object TryResolveService(Type desiredType)
    {
        if (desiredType == typeof(ILoggerFactory))
        {
            return NullLoggerFactory.Instance;
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
            return new MessageSerializationRegister(
                ResolveService<IMessageSubjectProvider>(),
                ResolveService<IMessageSerializationFactory>());
        }
        else if (desiredType == typeof(IMessageSubjectProvider))
        {
            return new NonGenericMessageSubjectProvider();
        }
        else if (desiredType == typeof(IQueueNamingConvention) || desiredType == typeof(ITopicNamingConvention))
        {
            return new DefaultNamingConventions();
        }

        return null;
    }
}