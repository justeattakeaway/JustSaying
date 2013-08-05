namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderFulfilmentStatus
{
    public class RequiresManualIntervention : Message
    {
        public int OrderId { get; private set; }

        public RequiresManualIntervention(int orderId)
        {
            OrderId = orderId;
        }
    }
}