using JustSaying.Models;

namespace JustSaying.IntegrationTests.TestHandlers;

public class OrderPlaced(string orderId) : Message
{
    public string OrderId { get; private set; } = orderId;
}