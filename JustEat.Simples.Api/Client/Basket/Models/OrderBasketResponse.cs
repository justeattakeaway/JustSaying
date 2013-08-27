using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class OrderBasketResponse
    {
        public string Id { get; set; }

        public decimal SubTotal { get; set; }

        public IList<UserPrompt> UserPrompt { get; set; }

        public IList<OrderItemView> OrderItems { get; set; }

        public int MenuId { get; set; }

        public decimal ToSpend { get; set; }

        public decimal MultiBuyDiscount { get; set; }

        public decimal Discount { get; set; }

        public decimal DeliveryCharge { get; set; }

        public decimal Total { get; set; }

        public bool Orderable { get; set; }

        public string ServiceType { get; set; }

        public int RestaurantId { get; set; }
    }
}