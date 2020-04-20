using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using HandlerFunc = System.Func<JustSaying.Models.Message, System.Threading.Tasks.Task<bool>>;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    public class HandlerMap
    {
        private readonly Dictionary<(string queueName, Type type), HandlerFunc> _handlers
            = new Dictionary<(string, Type), HandlerFunc>();

        public HandlerMap(
            IMessageMonitor messagingMonitor,
            ILoggerFactory loggerFactory)
        {
            MessagingMonitor = messagingMonitor;
            LoggerFactory = loggerFactory;
        }

        public bool Contains(string queueName, Type messageType)
            => _handlers.ContainsKey((queueName, messageType));

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
        public IMessageMonitor MessagingMonitor { get; }
        public ILoggerFactory LoggerFactory { get; }

        public void Add<T>(string queueName, Func<IHandlerAsync<T>> futureHandler) where T : Models.Message
        {
            var handlerWrapper = new MessageHandlerWrapper(MessageLock, MessagingMonitor, LoggerFactory);
            var handlerFunc = handlerWrapper.WrapMessageHandler(futureHandler);

            Add(queueName, typeof(T), handlerFunc);
        }

        /// <summary>
        /// Adds a handler to the handler map. If the handler is already registered for a queue, it will not be added again.
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

        public HandlerFunc Get(string queueName, Type messageType)
        {
            return _handlers.TryGetValue((queueName, messageType), out var handler) ? handler : null;
        }
    }
}
