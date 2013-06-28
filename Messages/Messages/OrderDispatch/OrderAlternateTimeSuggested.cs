using System;

namespace SimplesNotificationStack.Messaging.Messages.OrderDispatch
{
    public class OrderAlternateTimeSuggested : OrderMessage
    {
        public OrderAlternateTimeSuggested(int orderId, int customerId, int restaurantId, DateTime alternateTimeSuggestion)
            : base(orderId, customerId, restaurantId)
        {
            AlternateTimeSuggestion = alternateTimeSuggestion;
        }

        public DateTime AlternateTimeSuggestion { get; private set; }
    }
}