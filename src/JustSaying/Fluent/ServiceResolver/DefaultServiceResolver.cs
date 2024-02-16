using System.Runtime.CompilerServices;
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

#if NET8_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                #pragma warning disable IL2026
                #pragma warning disable IL3050
                return new NewtonsoftSerializationFactory();
                #pragma warning restore
            }
            else
            {
                throw new NotSupportedException($"Newtonsoft.Json is not supported when compiled with the 'PublishTrimmed' option. Use {nameof(TypedSystemTextJsonSerializationFactory)} instead.");
            }
#else
            return new NewtonsoftSerializationFactory();
#endif
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
