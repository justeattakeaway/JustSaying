using System;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying
{
    public class PredefinedHandlerResolver<TMessage> : IHandlerAndMetadataResolver
    {
        private readonly IHandlerAsync<TMessage> _handler;

        public PredefinedHandlerResolver(IHandlerAsync<TMessage> handler)
        {
            _handler = handler;
        }
        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContextWithMessage context)
        {
            return (IHandlerAsync<T>)_handler;
        }

        public Type ResolveHandlerType<T1>(HandlerResolutionContext context)
        {
            return _handler.GetType();
        }
    }
}