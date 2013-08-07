namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolutionRequested
{
    public abstract class OrderResolutionRequestedMessage : Message 
    {
        public int OrderId { get; private set; }

        public string AuditComment { get; private set; }

        public bool NotifiyCustomer { get; private set; }

        public OrderResolutionStatus OrderResolutionStatus { get; private set; }

        protected OrderResolutionRequestedMessage(int orderId, string auditComment, bool notifiyCustomer, OrderResolutionStatus orderResolutionStatus)
        {
            OrderResolutionStatus = orderResolutionStatus;
            NotifiyCustomer = notifiyCustomer;
            AuditComment = auditComment;
            OrderId = orderId;
        }
    }
}