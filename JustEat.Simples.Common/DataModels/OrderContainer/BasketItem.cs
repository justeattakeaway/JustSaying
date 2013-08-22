using System.Collections.Generic;

namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about single item in the basket. 
    /// </summary>
    public class BasketItem
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Product type.
        /// </summary>
        public int ProductTypeId { get; set; }

        /// <summary>
        /// Name of the product
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Additional info for the item. 
        /// </summary>
        public string Synonym { get; set; }

        /// <summary>
        /// Price of the given item.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Combined price of all items (including MealParts, OptionalAccessories and RequiredAccessories)
        /// </summary>
        public decimal CombinedPrice { get; set; }

        /// <summary>
        /// Meal deal. Either MealParts or (OptionalAccessories or RequiredAccessories) present in the item.
        /// </summary>
        public List<MealPart> MealParts { get; set; }

        /// <summary>
        /// Accessories added by customer. The item can be prepared without it.
        /// </summary>
        public List<OptionalAccessory> OptionalAccessories { get; set; }

        /// <summary>
        /// Accessories required by item. The item cannot be prepared without it.
        /// </summary>
        public List<RequiredAccessory> RequiredAccessories { get; set; }
    }
}
