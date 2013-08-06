namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved
{
    public class OrderResolvedAndCancelled : OrderResolvedMessage
    {
        public OrderResolvedAndCancelled(int orderId, bool notifiyCustomer, OrderResolutionStatus orderResolutionStatus)
            : base(orderId, notifiyCustomer, orderResolutionStatus)
        {
            
        }
    }
}