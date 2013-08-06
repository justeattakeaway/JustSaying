namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolutionRequested
{
    public class OrderAcceptedResolutionRequested : OrderResolutionRequestedMessage
    {
        public OrderAcceptedResolutionRequested(int orderId, string auditComment, bool notifiyCustomer,
                                        OrderResolutionStatus orderResolutionStatus)
            : base(orderId, auditComment, notifiyCustomer, orderResolutionStatus) {}
    }
}