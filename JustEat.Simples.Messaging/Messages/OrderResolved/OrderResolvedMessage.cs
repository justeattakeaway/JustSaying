namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved
{
    public class OrderResolvedMessage : Message 
    {
        public int OrderId { get; set; }
        
        public string AuditComment { get; set; }
        
        public bool NotifiyCustomer { get; set; }

        public OrderResolutionStatus OrderResolutionStatus { get; set; }

        public OrderResolvedMessage(int orderId, string auditComment, bool notifiyCustomer, OrderResolutionStatus orderResolutionStatus)
        {
            OrderResolutionStatus = orderResolutionStatus;
            NotifiyCustomer = notifiyCustomer;
            AuditComment = auditComment;
            OrderId = orderId;
        }
    }
}