using JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved;

namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderFulfilmentStatus
{
    public abstract class ResolvedOrderBase : Message
    {
        public int OrderId { get; private set; }
        public OrderResolutionStatus ResolutionStatus { get; private set; }

        public ResolvedOrderBase(int orderId, OrderResolutionStatus resolutionStatus)
        {
            ResolutionStatus = resolutionStatus;
            OrderId = orderId;
        }
    }
}