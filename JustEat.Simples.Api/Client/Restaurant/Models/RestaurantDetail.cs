using JustEat.Simples.Api.Client.Restaurant.Enums;

namespace JustEat.Simples.Api.Client.Restaurant.Models
{
    public class RestaurantDetail
    {
        public int RestaurantId { get; set; }

        public string Name { get; set; }

        public string PhoneNumber { get; set; }

        public int RestaurantType { get; set; }
    }

    public class OperationalStatus
    {
        public int RestaurantId { get; set; }
        public ConfidenceLevel Confidence { get; set; }
    }
}