using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Menu.Models
{
    public class ProductDetails
    {
        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; }
        public string Synonym { get; set; }
        public int ProductTypeId { get; set; }
        public bool RequireOtherProducts { get; set; }

        public Offer Offer { get; set; }

        public IList<AccessoryDetails> OptionalAccessories { get; set; }
        public IList<AccessoryDetails> RequiredAccessories { get; set; }
        public IList<MealPartDetails> MealParts { get; set; }
    }
}
