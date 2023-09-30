namespace JustSaying.Messaging.MessageHandling;

public class ListHandler<T>(IEnumerable<IHandlerAsync<T>> handlers) : IHandlerAsync<T>
{
    public async Task<bool> Handle(T message)
    {
        var handlerTasks = handlers.Select(h => h.Handle(message));
        var handlerResults = await Task.WhenAll(handlerTasks)
            .ConfigureAwait(false);

        return handlerResults.All(x => x);
    }
}
