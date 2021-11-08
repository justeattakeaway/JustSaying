using JustSaying.Messaging.MessageHandling;

namespace JustSaying;

public interface IHandlerResolver
{
    IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context);
}