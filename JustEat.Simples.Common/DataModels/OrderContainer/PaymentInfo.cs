using System;
using System.Collections.Generic;

namespace JustEat.Simples.Common.DataModels.OrderContainer
{
    /// <summary>
    /// Complete payment information for this order
    /// </summary>
    public class PaymentInfo
    {
        public string OrderId { get; set; }
        public List<PaymentLine> PaymentLines { get; set; }
        public decimal Total { get; set; }
        public DateTime? PaidDate { get; set; }
    }

    public class PaymentLine
    {
        public PaymentMethod Type { get; set; }
        public decimal Value { get; set; }
    }

    public class CardPaymentLine : PaymentLine
    {
        public string CardType { get; set; }
        public decimal CardFee { get; set; }
        public string LastCardDigits { get; set; }
        public string PspName { get; set; }
        public string PaymentTransactionRef { get; set; }
        public string AvsCheckInfo { get; set; }
    }

    public enum PaymentMethod { Cash = 1, Card, AccountCredit, Voucher }
}
