using JustSaying.Models;

namespace JustSaying.Sample.Restaurant.Models
{
    public class OrderPlacedEvent : Message
    {
        public int OrderId { get; set; }

        public string Description { get; set; }
    }
}
