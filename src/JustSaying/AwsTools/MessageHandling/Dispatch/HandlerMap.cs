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
    public class HandlerMap
    {
        private readonly IMessageMonitor _messageMonitor;
        private readonly ILoggerFactory _loggerFactory;

        private readonly Dictionary<(string queueName, Type type), HandlerFunc> _handlers
            = new Dictionary<(string, Type), HandlerFunc>();

        public HandlerMap(
            IMessageMonitor messageMonitor,
            ILoggerFactory loggerFactory)
        {
            _messageMonitor = messageMonitor;
            _loggerFactory = loggerFactory;
        }

        public IEnumerable<Type> Types
        {
            get
            {
                var types = new HashSet<Type>();
                foreach ((string queueName, Type type) key in _handlers.Keys)
                {
                    types.Add(key.type);
                }

                return types;
            }
        }

        public IMessageLockAsync MessageLock { get; set; }


        /// <summary>
        /// Adds a handler to be executed when a message arrives in a queue.
        /// If the handler is already registered for a queue, it will not be added again.
        /// </summary>
        /// <typeparam name="T">The type of the message to handle on this queue</typeparam>
        /// <param name="queueName">The queue to register the handler for</param>
        /// <param name="futureHandler">The factory function to create handlers with</param>
        public void Add<T>(string queueName, Func<IHandlerAsync<T>> futureHandler) where T : Models.Message
        {
            var handlerWrapper = new MessageHandlerWrapper(MessageLock, _messageMonitor, _loggerFactory);
            var handlerFunc = handlerWrapper.WrapMessageHandler(futureHandler);

            Add(queueName, typeof(T), handlerFunc);
        }

        /// <summary>
        /// Adds a handler to be executed when a message arrives in a queue.
        /// If the handler is already registered for a queue, it will not be added again.
        /// </summary>
        /// <param name="queueName">The queue name to register the handler for</param>
        /// <param name="messageType">The type of message to handle for this queue</param>
        /// <param name="handlerFunc">The provider of the handler to run for the queue/message type</param>
        public void Add(string queueName, Type messageType, HandlerFunc handlerFunc)
        {
            // We don't throw here when a handler has already been added, because sometimes callers want safe idempotent behaviour
            // e.g. when adding a handler for a queue who's name doesn't change between tenants.
            if (!_handlers.ContainsKey((queueName, messageType)))
            {
                _handlers.Add((queueName, messageType), handlerFunc);
            }
        }

        /// <summary>
        /// Gets a handler factory for a queue and message type
        /// </summary>
        /// <param name="queueName">The queue name to get the handler func for</param>
        /// <param name="messageType">The message type to get the handler func for</param>
        /// <returns></returns>
        public HandlerFunc Get(string queueName, Type messageType)
        {
            return _handlers.TryGetValue((queueName, messageType), out var handler) ? handler : null;
        }
    }
}
