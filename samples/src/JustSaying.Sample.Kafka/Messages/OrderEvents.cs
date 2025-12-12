using JustSaying.Models;

namespace JustSaying.Sample.Kafka.Messages;

/// <summary>
/// Sample event demonstrating CloudEvents compatibility with JustSaying Messages.
/// </summary>
public class OrderPlacedEvent : Message
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Sample event for order confirmation.
/// </summary>
public class OrderConfirmedEvent : Message
{
    public string OrderId { get; set; }
    public DateTime ConfirmedAt { get; set; }
    public string ConfirmedBy { get; set; }
}
