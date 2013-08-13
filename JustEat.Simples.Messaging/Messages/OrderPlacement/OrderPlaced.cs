using System;
namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class OrderPlaced : Message
    {
        public OrderPlaced(Guid orderId)
        {
            OrderId = orderId;
        }

        public Guid OrderId { get; private set; }
    }
}