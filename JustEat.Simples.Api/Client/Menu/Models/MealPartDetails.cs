using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Menu.Models
{
    public class MealPartDetails
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Synonym { get; set; }
        public IList<int> GroupIds { get; set; }
        public IList<AccessoryDetails> OptionalAccessories { get; set; }
        public IList<AccessoryDetails> RequiredAccessories { get; set; }
    }
}
