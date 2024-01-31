using JustSaying.Messaging.MessageHandling;

namespace JustSaying.TestingFramework;

public class DummyHandlerResolver<T> : IHandlerResolver
{
    private readonly IHandlerAsync<T> _handler;

    public DummyHandlerResolver(IHandlerAsync<T> handler)
    {
        _handler = handler;
    }

    public IHandlerAsync<TMessage> ResolveHandler<TMessage>(HandlerResolutionContext context)
    {
        return (IHandlerAsync<TMessage>)_handler;
    }
}
