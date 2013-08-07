namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved
{
    public class OrderResolvedAndAccepted : OrderResolvedMessage
    {
        public OrderResolvedAndAccepted(int orderId, bool notifiyCustomer,
                                        OrderResolutionStatus orderResolutionStatus)
            : base(orderId, notifiyCustomer, orderResolutionStatus) {}
    }
}