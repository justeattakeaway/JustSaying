using System.Security.Cryptography;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Restaurant.Models;

namespace JustSaying.Sample.Restaurant.OrderingApi.Handlers;

public class OrderDeliveredEventHandler(ILogger<OrderDeliveredEventHandler> logger) : IHandlerAsync<OrderDeliveredEvent>
{
    public async Task<bool> Handle(OrderDeliveredEvent message)
    {
        await Task.Delay(RandomNumberGenerator.GetInt32(50, 100));

        logger.LogInformation("Order {OrderId} has been delivered", message.OrderId);
        return true;
    }
}
