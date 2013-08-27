namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about customer.
    /// </summary>
    public class CustomerInfo
    {
        /// <summary>
        /// Customer Identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Customer Email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Customer name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Customer address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Customer city.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Customer postcode.
        /// </summary>
        public string Postcode { get; set; }

        /// <summary>
        /// Customer postcode.
        /// </summary>
        public string PhoneNumber { get; set; }

        public string TimeZone { get; set; }
    }
}
