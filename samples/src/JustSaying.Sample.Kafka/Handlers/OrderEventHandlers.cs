using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Kafka.Messages;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Kafka.Handlers;

/// <summary>
/// Handler for OrderPlacedEvent demonstrating CloudEvents consumption.
/// </summary>
public class OrderPlacedEventHandler : IHandlerAsync<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> Handle(OrderPlacedEvent message)
    {
        _logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}. Amount: {Amount:C}",
            message.OrderId,
            message.CustomerId,
            message.Amount);

        _logger.LogInformation(
            "Order metadata - RaisingComponent: {Component}, Tenant: {Tenant}, Timestamp: {Timestamp}",
            message.RaisingComponent,
            message.Tenant,
            message.TimeStamp);

        foreach (var item in message.Items)
        {
            _logger.LogInformation(
                "  Item: {ProductName} x {Quantity} @ {Price:C}",
                item.ProductName,
                item.Quantity,
                item.UnitPrice);
        }

        // Simulate processing
        await Task.Delay(100);

        _logger.LogInformation("Order {OrderId} processed successfully", message.OrderId);

        return true;
    }
}

/// <summary>
/// Handler for OrderConfirmedEvent.
/// </summary>
public class OrderConfirmedEventHandler : IHandlerAsync<OrderConfirmedEvent>
{
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(ILogger<OrderConfirmedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> Handle(OrderConfirmedEvent message)
    {
        _logger.LogInformation(
            "Order {OrderId} confirmed by {ConfirmedBy} at {ConfirmedAt}",
            message.OrderId,
            message.ConfirmedBy,
            message.ConfirmedAt);

        await Task.Delay(50);

        return true;
    }
}
