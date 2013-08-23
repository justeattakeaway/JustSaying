using System.Collections.Generic;

namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about restaurant.
    /// </summary>
    public class RestaurantInfo
    {
        /// <summary>
        /// Restaurant identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// restaurant name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Restaurant address lines.
        /// </summary>
        public List<string> AddressLines { get; set; }

        /// <summary>
        /// Restaurant city.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Restaurant Postcode1.
        /// </summary>
        public string Postcode1 { get; set; }

        /// <summary>
        /// Restaurant Postcode2.
        /// </summary>
        public string Postcode2 { get; set; }

        /// <summary>
        /// Restaurant phone number.
        /// </summary>
        public string PhoneNumber { get; set; }
    }
}
