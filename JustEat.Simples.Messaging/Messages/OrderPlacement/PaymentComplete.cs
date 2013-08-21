namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class PaymentComplete : Message
    {
        public PaymentComplete(string orderId, bool paymentSuccessful, string lastCardDigits, string avsStuff, string paymentTransactionRef, string paymentServiceProvider, decimal totalPaid)
        {
            TotalPaid = totalPaid;
            PaymentServiceProvider = paymentServiceProvider;
            PaymentTransactionRef = paymentTransactionRef;
            AvsStuff = avsStuff;
            LastCardDigits = lastCardDigits;
            PaymentSuccessful = paymentSuccessful;
            OrderId = orderId;
        }

        public string OrderId { get; private set; }
        public bool PaymentSuccessful { get; private set; }
        public string LastCardDigits { get; private set; }
        public string AvsStuff { get; private set; }
        public string PaymentTransactionRef { get; private set; }
        public string PaymentServiceProvider { get; private set; }
        public decimal TotalPaid { get; private set; }
    }
}