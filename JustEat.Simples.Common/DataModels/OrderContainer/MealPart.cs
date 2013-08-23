using System.Collections.Generic;

namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about which items included in the meal.
    /// </summary>
    public class MealPart
    {
        /// <summary>
        /// Meal part identifier.
        /// </summary>
        public int MealPartId { get; set; }

        /// <summary>
        /// Group identifier.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Name of the Meal Part.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Additional info for the item.
        /// </summary>
        public string Synonym { get; set; }

        /// <summary>
        /// Accessories which can be added by customer.
        /// </summary>
        public List<OptionalAccessory> OptionalAccessories { get; set; }

        /// <summary>
        /// Accessories must be included.
        /// </summary>
        public List<RequiredAccessory> RequiredAccessories { get; set; }
    }
}
