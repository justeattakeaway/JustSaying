using JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved;

namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderFulfilmentStatus
{
    public class Canceled : ResolvedOrderBase
    {
        public Canceled(int orderId, OrderResolutionStatus resolutionStatus) : base(orderId, resolutionStatus)
        {
        }
    }
}