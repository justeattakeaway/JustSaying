namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class DeleteMessageRequest
    {
        public DeleteMessageRequest(string queueUrl, string receiptHandle)
        {
            QueueUrl = queueUrl;
            ReceiptHandle = receiptHandle;
        }

        public string QueueUrl { get; }
        public string ReceiptHandle { get; }
    }
}
