namespace SimplesNotificationStack.Messaging.Messages.Sms
{
    public enum SmsCommunicationActivity { NotSent, Sent, ConfirmedReceived, FailedSending }
    public enum SmsCommunicationFailureReason { InvalidNumber, NoDeliveryReceipt }
}