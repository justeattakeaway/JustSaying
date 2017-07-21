using System;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying
{
    public interface IHandlerResolver
    {
        IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context);
    }

    public interface IHandlerAndMetadataResolver
    {
        IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContextWithMessage context);
        Type ResolveHandlerType<T>(HandlerResolutionContext context);
    }

    internal class HandlerResolverAdapter: IHandlerAndMetadataResolver
    {
        private readonly IHandlerResolver _legacyResolver;
        public HandlerResolverAdapter(IHandlerResolver legacyResolver)
        {
            _legacyResolver = legacyResolver;
        }

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContextWithMessage context)
        {
            return _legacyResolver.ResolveHandler<T>(context);
        }

        public Type ResolveHandlerType<T>(HandlerResolutionContext context)
        {
            return _legacyResolver.ResolveHandler<T>(context).GetType();
        }
    }
}