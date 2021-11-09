using JustSaying.Models;

namespace JustSaying.Sample.Restaurant.Models;

public class OrderOnItsWayEvent : Message
{
    public int OrderId { get; set; }
}