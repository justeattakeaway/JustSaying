using System.Collections.Generic;

namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Comprise information on which basis the order has been created.
    /// </summary>
    public class BasketInfo
    {
        /// <summary>
        /// Basket identifier.
        /// </summary>
        public string BasketId { get; set; }

        /// <summary>
        /// Menu card identifier.
        /// </summary>
        public int MenuId { get; set; }

        /// <summary>
        /// Collection of basket items.
        /// </summary>
        public List<BasketItem> BasketItem { get; set; }

        /// <summary>
        /// Subtotal of of the basket.
        /// </summary>
        public decimal SubTotal { get; set; } // Food total

        /// <summary>
        /// To spend.
        /// </summary>
        public decimal ToSpend { get; set; }

        /// <summary>
        /// MultyBuy discount.
        /// </summary>
        public decimal MultiBuyDiscount { get; set; }

        /// <summary>
        /// Discount.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Delivery charge.
        /// </summary>
        public decimal DeliveryCharge { get; set; }

        /// <summary>
        /// Total amount of the order.
        /// </summary>
        public decimal Total { get; set; } // SubTotal
    }
}
