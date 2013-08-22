namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class PaymentComplete : Message
    {
        public PaymentComplete(string orderId)
        {
            OrderId = orderId;
        }

        public string OrderId { get; private set; }
    }
}