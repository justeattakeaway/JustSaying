namespace JustSaying.Messaging.MessageHandling;

public class ListHandler<T> : IHandlerAsync<T>
{
    private readonly IEnumerable<IHandlerAsync<T>> _handlers;

    public ListHandler(IEnumerable<IHandlerAsync<T>> handlers)
    {
        _handlers = handlers;
    }

    public async Task<bool> Handle(T message)
    {
        var handlerTasks = _handlers.Select(h => h.Handle(message));
        var handlerResults = await Task.WhenAll(handlerTasks)
            .ConfigureAwait(false);

        return handlerResults.All(x => x);
    }
}