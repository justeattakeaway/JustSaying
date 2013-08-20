
using System.Collections.Generic;
namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class MealPart
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public IList<OptionalAccessory> OptionalAccessories { get; private set; }
        public IList<RequiredAccessory> RequiredAccessories { get; private set; }
    }
}