using JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved;

namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderFulfilmentStatus
{
    public class Accepted : ResolvedOrderBase
    {
        public Accepted(int orderId, OrderResolutionStatus resolutionStatus) : base(orderId, resolutionStatus)
        {
        }
    }
}