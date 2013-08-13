using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class OrderItemView
    {
        public string OrderItemId { get; set; }
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CombinedPrice { get; set; }
        public string Name { get; set; }
        public IList<MealPart> MealParts { get; set; }
        public IList<OptionalAccessory> OptionalAccessories { get; set; }
        public IList<RequiredAccessory> RequiredAccessories { get; set; }
        public string Synonym { get; set; }
        public int ProductTypeId { get; set; }
    }
}
