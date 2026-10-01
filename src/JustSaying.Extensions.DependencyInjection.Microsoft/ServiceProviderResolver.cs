using JustSaying.Extensions;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying;

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
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetRequiredService<ILogger<ServiceProviderResolver>>();
    }

    /// <summary>
    /// Gets the <see cref="ILogger"/> to use.
    /// </summary>
    private ILogger Logger { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> to use.
    /// </summary>
    private IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
    {
        bool logAtDebug = Logger.IsEnabled(LogLevel.Debug);

        if (logAtDebug)
        {
            Logger.LogDebug(
                "Resolving handler for message type {MessageType} for queue {QueueName}.",
                typeof(T).ToReadableFullName(),
                context.QueueName);
        }

        var handlers = ServiceProvider.GetServices<IHandlerAsync<T>>().ToArray();

        if (handlers.Length == 0)
        {
            throw new InvalidOperationException(
                $"No handler for message type {typeof(T).ToReadableFullName()} is registered.{GetRegistrationHint(typeof(T))}");
        }
        else if (handlers.Length > 1)
        {
            if (logAtDebug)
            {
                Logger.LogDebug(
                    "Resolved handler types for message type {MessageType} for queue {QueueName}: {ResolvedHandlerTypes}",
                    typeof(T).ToReadableFullName(),
                    context.QueueName,
                    string.Join(", ", handlers.Select((p) => p.GetType().ToReadableFullName())));
            }

            throw new NotSupportedException($"{handlers.Length} handlers for message type {typeof(T).ToReadableFullName()} are registered. Only one handler is supported per message type.");
        }

        var handler = handlers[0];

        if (logAtDebug)
        {
            Logger.LogDebug(
                "Resolved handler of type {ResolvedHandlerType} for queue {QueueName}.",
                handler.GetType().Name,
                context.QueueName);
        }

        return handler;
    }

    // A CloudEvents registration decides the handler's message type: HandlingCloudEvent<T> and
    // ForCloudEventTopic<T> deliver the CloudEvent<T> envelope, the ...Data variants the bare T. Name
    // the alternative when the envelope handler is missing, since the two are easy to mix up. Matched
    // by name, as this package doesn't reference JustSaying.CloudEvents.
    private static string GetRegistrationHint(Type messageType)
    {
        if (!messageType.IsGenericType
            || messageType.GetGenericTypeDefinition().FullName != "JustSaying.CloudEvents.CloudEvent`1")
        {
            return string.Empty;
        }

        var data = messageType.GetGenericArguments()[0].ToReadableName();
        return $" HandlingCloudEvent<{data}> and ForCloudEventTopic<{data}> deliver the envelope and so need an IHandlerAsync<{messageType.ToReadableName()}>; " +
               $"to handle just the data with an IHandlerAsync<{data}>, register HandlingCloudEventData<{data}> or ForCloudEventTopicData<{data}> instead.";
    }

    /// <inheritdoc />
    public T ResolveService<T>() where T : class => ServiceProvider.GetRequiredService<T>();

    public T ResolveOptionalService<T>() where T : class => ServiceProvider.GetService<T>();
}
