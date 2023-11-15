using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.TestHandlers;

public class BlockingOrderProcessor : IHandlerAsync<OrderPlaced>
{
    public int ReceivedMessageCount { get; private set; }

    public TaskCompletionSource<object> DoneSignal { get; private set; }

    public Task<bool> Handle(OrderPlaced message)
    {
        ReceivedMessageCount++;
        return Task.FromResult(true);
    }
}
