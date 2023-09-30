using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.TestHandlers;

public class OrderDispatcher(Future<OrderPlaced> future) : IHandlerAsync<OrderPlaced>
{
    public async Task<bool> Handle(OrderPlaced message)
    {
        await Future.Complete(message);
        return true;
    }

    public Future<OrderPlaced> Future { get; } = future;
}