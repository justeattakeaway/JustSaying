using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using HandlerFunc = System.Func<JustSaying.Models.Message, System.Threading.Tasks.Task<bool>>;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    /// <summary>
    /// A <see cref="HandlerMap"/> is a register of handlers keyed by type and queue. Calling <see cref="Add"/>
    /// with a queue name, type, and handler will cause the handler to be called when a message matching the type
    /// arrives in the queue.
    /// </summary>
    public sealed class HandlerMap
    {
        private readonly IMessageMonitor _messageMonitor;
        private readonly ILoggerFactory _loggerFactory;

        private readonly Dictionary<(string queueName, Type type), HandlerFunc> _handlers
            = new Dictionary<(string, Type), HandlerFunc>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerMap"/> class.
        /// </summary>
        /// <param name="messageMonitor">The <see cref="IMessageMonitor"/> used to wrap handlers in <see cref="MessageHandlerWrapper"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
        public HandlerMap(
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
        /// <returns>Returns true if handler has been registered for the queue.</returns>
        public bool Contains(string queueName, Type messageType)
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (messageType is null) throw new ArgumentNullException(nameof(messageType));

            return _handlers.ContainsKey((queueName, messageType));
        }

        /// <summary>
        /// Types returns a unique list of types that are handled by all queues.
        /// </summary>
        public IEnumerable<Type> Types
        {
            get
            {
                var types = new HashSet<Type>();
                foreach ((var _, Type type) in _handlers.Keys)
                {
                    types.Add(type);
                }

                return types;
            }
        }

        /// <summary>
        /// MessageLock assigns the <see cref="IMessageLockAsync"/> to be used by <see cref="MessageHandlerWrapper"/>.
        /// </summary>
        public IMessageLockAsync MessageLock { get; set; }

        /// <summary>
        /// Adds a handler to be executed when a message arrives in a queue.
        /// If the handler is already registered for a queue, it will not be added again.
        /// </summary>
        /// <typeparam name="T">The type of the message to handle on this queue.</typeparam>
        /// <param name="queueName">The queue to register the handler for.</param>
        /// <param name="futureHandler">The factory function to create handlers with.</param>
        public HandlerMap Add<T>(string queueName, Func<IHandlerAsync<T>> futureHandler) where T : Models.Message
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (futureHandler is null) throw new ArgumentNullException(nameof(futureHandler));

            var handlerWrapper = new MessageHandlerWrapper(MessageLock, _messageMonitor, _loggerFactory);
            var handlerFunc = handlerWrapper.WrapMessageHandler(futureHandler);

            return Add(queueName, typeof(T), handlerFunc);
        }

        /// <summary>
        /// Adds a handler to be executed when a message arrives in a queue.
        /// The last handler registered for a given queue will be used.
        /// </summary>
        /// <param name="queueName">The queue name to register the handler for.</param>
        /// <param name="messageType">The type of message to handle for this queue.</param>
        /// <param name="handlerFunc">The provider of the handler to run for the queue/message type.</param>
        public HandlerMap Add(string queueName, Type messageType, HandlerFunc handlerFunc)
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (messageType is null) throw new ArgumentNullException(nameof(messageType));
            if (handlerFunc is null) throw new ArgumentNullException(nameof(handlerFunc));

            _handlers[(queueName, messageType)] = handlerFunc;

            return this;
        }

        /// <summary>
        /// Gets a handler factory for a queue and message type.
        /// </summary>
        /// <param name="queueName">The queue name to get the handler function for.</param>
        /// <param name="messageType">The message type to get the handler function for.</param>
        /// <returns>The registered handler or null.</returns>
        public HandlerFunc Get(string queueName, Type messageType)
        {
            if (queueName is null) throw new ArgumentNullException(nameof(queueName));
            if (messageType is null) throw new ArgumentNullException(nameof(messageType));

            return _handlers.TryGetValue((queueName, messageType), out var handler) ? handler : null;
        }
    }
}
