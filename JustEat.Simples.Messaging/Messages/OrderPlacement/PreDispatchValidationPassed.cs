namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class PreDispatchValidationPassed : Message
    {
        public PreDispatchValidationPassed(string orderId)
        {
            OrderId = orderId;
        }

        public string OrderId { get; private set; }
    }
}