using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.Handle;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.Handle.HandleMessageContext, bool>;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    /// <summary>
    /// A <see cref="MiddlewareMap"/> is a register of handlers keyed by type and queue. Calling <see cref="Add"/>
    /// with a queue name, type, and handler will cause the handler to be called when a message matching the type
    /// arrives in the queue.
    /// </summary>
    public sealed class MiddlewareMap
    {
        private readonly IMessageMonitor _messageMonitor;
        private readonly ILoggerFactory _loggerFactory;

        private readonly Dictionary<(string queueName, Type type), Func<HandleMessageMiddleware>> _middlewares
            = new Dictionary<(string, Type), Func<HandleMessageMiddleware>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MiddlewareMap"/> class.
        /// </summary>
        /// <param name="messageMonitor">The <see cref="IMessageMonitor"/> used to wrap handlers in <see cref="MessageHandlerWrapper"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
        public MiddlewareMap(
            IMessageMonitor messageMonitor,
            ILoggerFactory loggerFactory)
        {
            _messageMonitor = messageMonitor ?? throw new ArgumentNullException(nameof(messageMonitor));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Checks if a handler has been added for a given queue and message type.
        /// </summary>
        /// <param name="queueName">The queue name to register the handler for.</param>
        /// <param name="messageType">The type of message to handle for this queue.</param>
        /// <returns>Returns true if the handler has been registered for the queue.</returns>
        public bool Contains(string queueName, Type messageType)
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (messageType is null) throw new ArgumentNullException(nameof(messageType));

            return _middlewares.ContainsKey((queueName, messageType));
        }

        /// <summary>
        /// Gets a unique list of types that are handled by all queues.
        /// </summary>
        public IEnumerable<Type> Types
        {
            get
            {
                var types = new HashSet<Type>();
                foreach ((var _, Type type) in _middlewares.Keys)
                {
                    types.Add(type);
                }

                return types;
            }
        }

        /// <summary>
        /// Gets the <see cref="IMessageLockAsync"/> to be used by the <see cref="MessageHandlerWrapper"/>.
        /// </summary>
        public IMessageLockAsync MessageLock { get; set; }

        /// <summary>
        /// Adds a handler to be executed when a message arrives in a queue.
        /// If the handler is already registered for a queue, it will not be added again.
        /// </summary>
        /// <typeparam name="T">The type of the message to handle on this queue.</typeparam>
        /// <param name="queueName">The queue to register the handler for.</param>
        /// <param name="middleware">The factory function to create handlers with.</param>
        public MiddlewareMap Add<T>(string queueName, Func<HandleMessageMiddleware> middleware) where T : Message
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (middleware is null) throw new ArgumentNullException(nameof(middleware));

            // TODO: reimplement as middleware
            //var handlerWrapper = new MessageHandlerWrapper(MessageLock, _messageMonitor, _loggerFactory);
            //var handlerFunc = handlerWrapper.WrapMessageHandler(futureHandler);

            _middlewares[(queueName, typeof(T))] = middleware;

            return this;
        }

        /// <summary>
        /// Gets a handler factory for a queue and message type.
        /// </summary>
        /// <param name="queueName">The queue name to get the handler function for.</param>
        /// <param name="messageType">The message type to get the handler function for.</param>
        /// <returns>The registered handler or null.</returns>
        public Func<HandleMessageMiddleware> Get(string queueName, Type messageType)
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (messageType is null) throw new ArgumentNullException(nameof(messageType));

            return _middlewares.TryGetValue((queueName, messageType), out var handler) ? handler : null;
        }
    }
}
