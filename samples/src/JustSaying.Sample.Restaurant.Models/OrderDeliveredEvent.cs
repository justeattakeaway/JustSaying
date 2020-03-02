using JustSaying.Models;

namespace JustSaying.Sample.Restaurant.Models
{
    public class OrderDeliveredEvent : Message
    {
        public int OrderId { get; set; }
    }
}
