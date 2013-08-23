namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about accessories which must be included in the item.
    /// </summary>
    public class RequiredAccessory
    {
        /// <summary>
        /// Required Accessory identifier
        /// </summary>
        public int RequiredAccessoryId { get; set; }

        /// <summary>
        /// Croup this item belongs to identifier
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Price of the single accessory.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Name of the Required Accessory
        /// </summary>
        public string Name { get; set; }
    }
}
