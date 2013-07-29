namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved
{
    public class OrderResolvedAndCancelled : OrderResolvedMessage
    {
        public OrderResolvedAndCancelled(int orderId, string auditComment, bool notifiyCustomer, OrderResolutionStatus orderResolutionStatus)
            : base(orderId, auditComment, notifiyCustomer, orderResolutionStatus)
        {
            
        }
    }
}