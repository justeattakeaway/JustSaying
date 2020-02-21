using System;
using System.Collections.Generic;
using System.Linq;
using HandlerFunc = System.Func<JustSaying.Models.Message, System.Threading.Tasks.Task<bool>>;

namespace JustSaying.AwsTools.MessageHandling
{
    public class HandlerMap
    {
        private readonly Dictionary<Type, HandlerFunc> _handlers = new Dictionary<Type, HandlerFunc>();

        public bool ContainsKey(Type messageType) => _handlers.ContainsKey(messageType);

        public IEnumerable<Type> Types => _handlers.Keys;

        public void Add(Type messageType, HandlerFunc handlerFunc) => _handlers.Add(messageType, handlerFunc);

        public HandlerFunc Get(Type messageType)
        {
            HandlerFunc handler;
            return _handlers.TryGetValue(messageType, out handler) ? handler : null;
        }
    }
}
