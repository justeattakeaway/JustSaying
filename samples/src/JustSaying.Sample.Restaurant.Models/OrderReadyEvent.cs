using JustSaying.Models;

namespace JustSaying.Sample.Restaurant.Models;

public class OrderReadyEvent : Message
{
    public int OrderId { get; set; }
}