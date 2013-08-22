using System;

namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information about order.
    /// </summary>
    public class OrderInfo
    {
        /// <summary>
        /// restaurant note made by customer for the whole order.
        /// </summary>
        public string NoteToRestaurant { get; set; }

        /// <summary>
        /// Type of the order.
        /// </summary>
        public ServiceType ServiceType { get; set; }

        /// <summary>
        /// Date when order has been created. (Not basket creation time).
        /// </summary>
        public DateTime PlacedDate { get; set; }

        /// <summary>
        /// Date when order has been paid.
        /// </summary>
        public DateTime PaidDate { get; set; }

        /// <summary>
        /// Initial due (delivery) date and time.
        /// </summary>
        /// TODO needs to get from basket. Awaiting http://jira.just-eat.net:8080/browse/JAL-572.
        public DateTime InitialDueDate { get; set; }

        /// <summary>
        /// Due (delivery) date. Actual date and time
        /// </summary>
        public DateTime DueDate { get; set; }
    }
}
