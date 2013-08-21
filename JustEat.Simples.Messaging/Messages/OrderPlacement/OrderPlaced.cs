using System;
namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class OrderPlaced : Message
    {
        public OrderPlaced(Guid orderId, int legacyrderId)
        {
            LegacyrderId = legacyrderId;
            OrderId = orderId;
        }

        public Guid OrderId { get; private set; }
        public int LegacyrderId { get; private set; }
    }
}