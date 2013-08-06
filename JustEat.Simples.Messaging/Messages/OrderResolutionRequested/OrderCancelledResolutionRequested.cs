namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolutionRequested
{
    public class OrderCancelledResolutionRequested : OrderResolutionRequestedMessage
    {
        public OrderCancelledResolutionRequested(int orderId, string auditComment, bool notifiyCustomer, OrderResolutionStatus orderResolutionStatus)
            : base(orderId, auditComment, notifiyCustomer, orderResolutionStatus)
        {
            
        }
    }
}