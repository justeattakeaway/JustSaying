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

        public void Add(string queueName, Type messageType, HandlerFunc handlerFunc)
        {
            if (_handlers.ContainsKey((queueName, messageType)))
            {
                throw new InvalidOperationException(
                    $"A message handler has already been registered for type {messageType.FullName}");
            }

            _handlers.Add((queueName, messageType), handlerFunc);
        }

        public HandlerFunc Get(string queueName, Type messageType)
        {
            return _handlers.TryGetValue((queueName, messageType), out var handler) ? handler : null;
        }
    }
}
