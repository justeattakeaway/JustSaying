namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class OrderPlaced : Message
    {
        public OrderPlaced(string orderId, int legacyOrderId)
        {
            LegacyOrderId = legacyOrderId;
            OrderId = orderId;
        }

        public string OrderId { get; private set; }
        public int LegacyOrderId { get; private set; }
    }
}