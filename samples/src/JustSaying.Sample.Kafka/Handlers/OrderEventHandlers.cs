using System.Security.Cryptography;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Kafka.Messages;

namespace JustSaying.Sample.Kafka.Handlers;

/// <summary>
/// Handler for OrderPlacedEvent demonstrating CloudEvents consumption with retry/DLT support.
/// </summary>
public class OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger, IMessagePublisher publisher) : IHandlerAsync<OrderPlacedEvent>
{
    // Simulate transient failures - in production this would be actual business logic errors
    private static readonly Random _random = new();
    
    // Track retry attempts per order (in production, use distributed state or message headers)
    private static readonly Dictionary<string, int> _attemptTracker = new();
    private static readonly object _lock = new();
    
    public async Task<bool> Handle(OrderPlacedEvent message)
    {
        // Track attempts for this order
        int attemptNumber;
        lock (_lock)
        {
            if (!_attemptTracker.TryGetValue(message.OrderId, out attemptNumber))
            {
                attemptNumber = 0;
            }
            attemptNumber++;
            _attemptTracker[message.OrderId] = attemptNumber;
        }
        
        logger.LogInformation(
            "Processing order {OrderId} (attempt {Attempt}) for customer {CustomerId}. Amount: {Amount:C}",
            message.OrderId,
            attemptNumber,
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
        
        // ===== SIMULATE FAILURES FOR DLT DEMONSTRATION =====
        // Orders with amount > 500 have a 70% chance of transient failure (to demonstrate retries)
        // Orders with amount > 1000 always fail (to demonstrate DLT)
        if (message.Amount > 1000)
        {
            logger.LogError(
                "Order {OrderId} FAILED - Amount {Amount:C} exceeds processing limit. " +
                "This order will be sent to DLT after all retries are exhausted.",
                message.OrderId, message.Amount);
            
            // Throw exception to trigger retry mechanism
            throw new InvalidOperationException($"Order amount ${message.Amount} exceeds maximum allowed limit of $1000");
        }
        
        if (message.Amount > 500 && _random.NextDouble() < 0.7 && attemptNumber < 3)
        {
            logger.LogWarning(
                "Order {OrderId} TRANSIENT FAILURE (attempt {Attempt}) - Simulating temporary error. Will retry...",
                message.OrderId, attemptNumber);
            
            // Return false to indicate failure and trigger retry
            return false;
        }
        // ===== END SIMULATION =====

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

        logger.LogInformation("Order {OrderId} processed successfully and confirmed after {Attempts} attempt(s)", 
            message.OrderId, attemptNumber);
        
        // Clean up tracking
        lock (_lock)
        {
            _attemptTracker.Remove(message.OrderId);
        }

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
