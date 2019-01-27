using System;
using System.Linq;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
                    typeof(T).FullName,
                    context.QueueName);
            }

            var handlers = ServiceProvider.GetServices<IHandlerAsync<T>>().ToArray();

            if (handlers.Length == 0)
            {
                throw new NotSupportedException($"No handler for message type {typeof(T).FullName} is registered.");
            }
            else if (handlers.Length > 1)
            {
                if (logAtDebug)
                {
                    Logger.LogDebug(
                        "Resolved handler types for message type {MessageType} for queue {QueueName}: {ResolvedHandlerTypes}",
                        typeof(T).FullName,
                        context.QueueName,
                        string.Join(", ", handlers.Select((p) => p.GetType().FullName)));
                }

                throw new NotSupportedException($"{handlers.Length} handlers for message type {typeof(T).FullName} are registered. Only one handler is supported per message type.");
            }

            var handler = handlers[0];

            if (logAtDebug)
            {
                Logger.LogDebug(
                    "Resolved handler of type {ResolvedHandlerType} for queue {QueueName}.",
                    handler.GetType().FullName,
                    context.QueueName);
            }

            return handler;
        }

        /// <inheritdoc />
        public T ResolveService<T>()
            => ServiceProvider.GetRequiredService<T>();
    }
}
