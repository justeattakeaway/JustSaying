namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeDeleteMessageRequest
{
    public FakeDeleteMessageRequest(string queueUrl, string receiptHandle)
    {
        QueueUrl = queueUrl;
        ReceiptHandle = receiptHandle;
    }

    public string QueueUrl { get; }
    public string ReceiptHandle { get; }
}