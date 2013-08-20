namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class PaymentComplete : Message
    {
        public string OrderId { get; set; }
        public bool PaymentSuccessful { get; set; }
        public string LastCardDigits { get; set; }
        public string AvsStuff { get; set; }
        public string PaymentTransactionRef { get; set; }
        public string PaymentServiceProvider { get; set; }
        public double TotalPaid { get; set; }
    }
}