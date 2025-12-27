using System.Security.Cryptography;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Kafka.Messages;

namespace JustSaying.Sample.Kafka.Handlers;

/// <summary>
/// Handler for OrderPlacedEvent demonstrating CloudEvents consumption.
/// </summary>
public class OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger, IMessagePublisher publisher) : IHandlerAsync<OrderPlacedEvent>
{
    public async Task<bool> Handle(OrderPlacedEvent message)
    {
        logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}. Amount: {Amount:C}",
            message.OrderId,
            message.CustomerId,
            message.Amount);

        logger.LogInformation(
            "Order metadata - RaisingComponent: {Component}, Tenant: {Tenant}, Timestamp: {Timestamp}",
            message.RaisingComponent,
            message.Tenant,
            message.TimeStamp);

        foreach (var item in message.Items)
        {
            logger.LogInformation(
                "  Item: {ProductName} x {Quantity} @ {Price:C}",
                item.ProductName,
                item.Quantity,
                item.UnitPrice);
        }

        // Simulate some processing
        await Task.Delay(RandomNumberGenerator.GetInt32(50, 150));

        // Publish confirmation event
        var orderConfirmed = new OrderConfirmedEvent
        {
            OrderId = message.OrderId,
            ConfirmedAt = DateTime.UtcNow,
            ConfirmedBy = "OrderProcessingSystem",
            RaisingComponent = "KafkaOrderingApi",
            Tenant = message.Tenant
        };

        await publisher.PublishAsync(orderConfirmed);

        logger.LogInformation("Order {OrderId} processed successfully and confirmed", message.OrderId);

        // Returning true indicates:
        //   The message was handled successfully
        //   The message can be committed in Kafka
        // Returning false would indicate:
        //   The message was not handled successfully
        //   The message handling should be retried
        return true;
    }
}

/// <summary>
/// Handler for OrderConfirmedEvent.
/// </summary>
public class OrderConfirmedEventHandler(ILogger<OrderConfirmedEventHandler> logger) : IHandlerAsync<OrderConfirmedEvent>
{
    public async Task<bool> Handle(OrderConfirmedEvent message)
    {
        logger.LogInformation(
            "Order {OrderId} confirmed by {ConfirmedBy} at {ConfirmedAt}",
            message.OrderId,
            message.ConfirmedBy,
            message.ConfirmedAt);

        // This is where you would update order status in database
        // Send notification to customer, etc.
        // Intentionally left empty for the sake of this being a sample application

        await Task.Delay(RandomNumberGenerator.GetInt32(25, 75));

        logger.LogInformation("Order {OrderId} confirmation processed", message.OrderId);

        return true;
    }
}
