namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about accessories which can be added by customer.
    /// </summary>
    public class OptionalAccessory
    {
        /// <summary>
        /// Optional Accessory Identifier.
        /// </summary>
        public int OptionalAccessoryId { get; set; }

        /// <summary>
        /// Quantity of accessories to be added.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Price of the single accessory.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Name of the Optional Accessory.
        /// </summary>
        public string Name { get; set; }
    }
}
