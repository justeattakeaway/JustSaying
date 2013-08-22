namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class PaymentComplete : Message
    {
        public PaymentComplete(string orderId, string compressedOrderJson)
        {
            CompressedOrderJson = compressedOrderJson;
            OrderId = orderId;
        }

        public string OrderId { get; private set; }
        public string CompressedOrderJson { get; private set; }
    }
}