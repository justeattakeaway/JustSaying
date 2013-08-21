using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Menu.Models
{
    public class MealPartDetails
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Synonym { get; set; }
        public IEnumerable<int> GroupIds { get; set; }
        public IEnumerable<AccessoryDetails> OptionalAccessories { get; set; }
        public IEnumerable<AccessoryDetails> RequiredAccessories { get; set; }
    }
}
