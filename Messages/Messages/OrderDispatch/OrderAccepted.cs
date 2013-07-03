namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch
{
    public class OrderAccepted : OrderMessage
    {
        public OrderAccepted(int orderId, int customerId, int restaurantId) : base(orderId, customerId, restaurantId)
        {
        }
    }
}