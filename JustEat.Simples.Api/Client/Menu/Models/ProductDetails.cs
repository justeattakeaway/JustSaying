using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Menu.Models
{
    public class ProductDetails
    {
        public ProductDetails()
        {
            OptionalAccessories = new List<AccessoryDetails>();
            RequiredAccessories = new List<AccessoryDetails>();
            MealParts = new List<MealPartDetails>();
        }

        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; }
        public string Synonym { get; set; }
        public int ProductTypeId { get; set; }
        public bool RequireOtherProducts { get; set; }

        public Offer Offer { get; set; }

        public IEnumerable<AccessoryDetails> OptionalAccessories { get; set; }
        public IEnumerable<AccessoryDetails> RequiredAccessories { get; set; }
        public IEnumerable<MealPartDetails> MealParts { get; set; }

        public bool HasMealParts { get; set; }
    }
}
