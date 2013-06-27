namespace SimplesNotificationStack.Messaging.Messages.OrderDispatch
{
    public abstract class OrderMessage : Message
    {
        public OrderMessage(int orderId, int customerId, int restaurantId)
        {
            RestaurantId = restaurantId;
            CustomerId = customerId;
            OrderId = orderId;
        }

        public int OrderId { get; private set; }
        public int CustomerId { get; private set; }
        public int RestaurantId { get; private set; }
    }
}