namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch
{
    public class OrderRejected : OrderMessage
    {
        public OrderRejected(int orderId, int customerId, int restaurantId, OrderRejectReason rejectReason)
            : base(orderId, customerId, restaurantId)
        {
            RejectReason = rejectReason;
        }

        public OrderRejectReason RejectReason { get; private set; }
    }
}