using System.Security.Cryptography;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Restaurant.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Restaurant.KitchenConsole.Handlers;

public class OrderOnItsWayEventHandler(IMessagePublisher publisher, ILogger<OrderOnItsWayEventHandler> logger) : IHandlerAsync<OrderOnItsWayEvent>
{
    public async Task<bool> Handle(OrderOnItsWayEvent message)
    {
        await Task.Delay(RandomNumberGenerator.GetInt32(50, 100));

        var orderDeliveredEvent = new OrderDeliveredEvent()
        {
            OrderId = message.OrderId
        };

        logger.LogInformation("Order {OrderId} is on its way!", message.OrderId);

        await publisher.PublishAsync(orderDeliveredEvent);

        return true;
    }
}
