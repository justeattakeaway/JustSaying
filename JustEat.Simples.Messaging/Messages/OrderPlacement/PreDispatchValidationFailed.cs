namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class PreDispatchValidationFailed : Message
    {
        public PreDispatchValidationFailed(string orderId, PreDispatchValidationFailureReason failureReason)
        {
            OrderId = orderId;
            FailureReason = failureReason;
        }

        public string OrderId { get; private set; }
        public PreDispatchValidationFailureReason FailureReason { get; private set; }
    }
}