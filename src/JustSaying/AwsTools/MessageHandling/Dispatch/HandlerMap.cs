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
        private readonly Dictionary<Type, HandlerFunc> _handlers = new Dictionary<Type, HandlerFunc>();

        public HandlerMap(
            IMessageMonitor messagingMonitor,
            ILoggerFactory loggerFactory)
        {
            MessagingMonitor = messagingMonitor;
            LoggerFactory = loggerFactory;
        }

        public bool ContainsKey(Type messageType) => _handlers.ContainsKey(messageType);

        public IEnumerable<Type> Types => _handlers.Keys;

        public IMessageLockAsync MessageLock { get; set; }
        public IMessageMonitor MessagingMonitor { get; }
        public ILoggerFactory LoggerFactory { get; }

        public void Add<T>(Func<IHandlerAsync<T>> futureHandler) where T : Models.Message
        {
            var handlerWrapper = new MessageHandlerWrapper(MessageLock, MessagingMonitor, LoggerFactory);
            var handlerFunc = handlerWrapper.WrapMessageHandler(futureHandler);

            Add(typeof(T), handlerFunc);
        }

        public void Add(Type messageType, HandlerFunc handlerFunc)
        {
            _handlers.Add(messageType, handlerFunc);
        }

        public HandlerFunc Get(Type messageType)
        {
            HandlerFunc handler;
            return _handlers.TryGetValue(messageType, out handler) ? handler : null;
        }
    }
}
