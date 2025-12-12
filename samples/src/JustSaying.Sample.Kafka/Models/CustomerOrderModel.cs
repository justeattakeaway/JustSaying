namespace JustSaying.Sample.Kafka.Models;

/// <summary>
/// Represents a customer order request received from the API
/// </summary>
public class CustomerOrderModel
{
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public List<OrderItemModel> Items { get; set; } = new();
}

/// <summary>
/// Represents an item in a customer order
/// </summary>
public class OrderItemModel
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
