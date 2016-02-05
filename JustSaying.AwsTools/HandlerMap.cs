using System;
using System.Collections.Generic;
using HandlerFunc = System.Func<JustSaying.Models.Message, bool>;

namespace JustSaying.AwsTools
{
    public class HandlerMap
    {
        private readonly Dictionary<Type, List<HandlerFunc>> _handlers = new Dictionary<Type, List<HandlerFunc>>();

        public void Add(Type messageType, HandlerFunc handlerFunc)
        {
            List<HandlerFunc> handlersForType;
            if (!_handlers.TryGetValue(messageType, out handlersForType))
            {
                handlersForType = new List<HandlerFunc>();
                _handlers.Add(messageType, handlersForType);
            }

            handlersForType.Add(handlerFunc);
        }

        public List<HandlerFunc> Get(Type messageType)
        {
            List<HandlerFunc> handlers;
            if (_handlers.TryGetValue(messageType, out handlers))
            {
                return handlers;
            }

            return null;
        }
    }
}
