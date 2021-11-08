using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.TestHandlers;

public class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
{
    public Task<bool> Handle(OrderPlaced message)
        => Task.FromResult(true);
}