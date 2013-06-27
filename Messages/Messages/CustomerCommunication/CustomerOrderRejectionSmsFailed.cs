using SimplesNotificationStack.Messaging.Messages.Sms;

namespace SimplesNotificationStack.Messaging.Messages.CustomerCommunication
{
    public class CustomerOrderRejectionSmsFailed : CustomerOrderRejectionSms
    {
        public CustomerOrderRejectionSmsFailed(int orderId, int customerId, string telephoneNumber, SmsCommunicationActivity communicationActivity, SmsCommunicationFailureReason failureReason, string failureDetails)
            : base(orderId, customerId, telephoneNumber, communicationActivity)
        {
            FailureDetails = failureDetails;
            FailureReason = failureReason;
        }

        public SmsCommunicationFailureReason FailureReason { get; private set; }
        public string FailureDetails { get; private set; }
    }
}