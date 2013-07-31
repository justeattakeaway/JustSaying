namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved
{
    public class OrderResolvedAndAccepted : OrderResolvedMessage
    {
        public OrderResolvedAndAccepted(int orderId, string auditComment, bool notifiyCustomer,
                                        OrderResolutionStatus orderResolutionStatus)
            : base(orderId, auditComment, notifiyCustomer, orderResolutionStatus) {}
    }
}