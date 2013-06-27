using System;

namespace SimplesNotificationStack.Messaging.Messages.OrderDispatch
{
    public class OrderAlternateTimeSuggested : OrderMessage
    {
        public OrderAlternateTimeSuggested(int orderId, int customerId, int restaurantId, DateTimeOffset alternateTimeSuggestion)
            : base(orderId, customerId, restaurantId)
        {
            AlternateTimeSuggestion = alternateTimeSuggestion;
        }

        public DateTimeOffset AlternateTimeSuggestion { get; private set; }
    }
}