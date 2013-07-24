using System;

namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch
{
    public class OrderAlternateTimeSuggested : OrderMessage
    {
        public OrderAlternateTimeSuggested(int orderId, int customerId, int restaurantId, DateTime alternateTimeSuggestion)
            : base(orderId, customerId, restaurantId)
        {
            AlternateLocalTimeSuggestion = alternateTimeSuggestion;
        }

        public DateTime AlternateLocalTimeSuggestion { get; private set; }
    }
}