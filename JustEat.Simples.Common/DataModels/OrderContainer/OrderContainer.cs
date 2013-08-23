namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about current order state. And represent last state after order processing completed.
    /// </summary>
    public class OrderContainer
    {
        /// <summary>
        /// Order identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Order identifier (legacy). Kept for backward compatibility.
        /// </summary>
        public int LegacyId { get; set; }

        /// <summary>
        /// Information about which application initiated order placement.
        /// </summary>
        public ApplicationInfo ApplicationInfo { get; set; }

        /// <summary>
        /// Order related information.
        /// </summary>
        public OrderInfo Order{ get; set; }

        /// <summary>
        /// Restaurant related information.
        /// </summary>
        public RestaurantInfo RestaurantInfo { get; set; }

        /// <summary>
        /// Payment related information.
        /// </summary>
        public PaymentInfo PaymentInfo { get; set; }

        /// <summary>
        /// Customer related information.
        /// </summary>
        public CustomerInfo CustomerInfo { get; set; }

        /// <summary>
        /// Basket related information
        /// </summary>
        public BasketInfo BasketInfo { get; set; }
    }
}