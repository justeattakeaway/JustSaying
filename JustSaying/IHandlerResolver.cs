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
}