namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved
{
    public abstract class OrderResolvedMessage : Message 
    {
        public int OrderId { get; private set; }

        public bool NotifiyCustomer { get; private set; }

        public OrderResolutionStatus OrderResolutionStatus { get; private set; }

        protected OrderResolvedMessage(int orderId, bool notifiyCustomer, OrderResolutionStatus orderResolutionStatus)
        {
            OrderResolutionStatus = orderResolutionStatus;
            NotifiyCustomer = notifiyCustomer;
            OrderId = orderId;
        }
    }
}