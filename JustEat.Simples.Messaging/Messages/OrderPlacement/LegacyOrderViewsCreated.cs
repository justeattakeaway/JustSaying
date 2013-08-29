namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class LegacyOrderViewsCreated : Message
    {
        public LegacyOrderViewsCreated(string orderId, int legacyOrderId)
        {
            OrderId = orderId;
            LegacyOrderId = legacyOrderId;
        }

        public string OrderId { get; private set; }
        public int LegacyOrderId { get; private set; }
    }
}