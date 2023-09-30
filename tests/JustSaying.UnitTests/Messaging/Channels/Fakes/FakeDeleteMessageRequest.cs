namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeDeleteMessageRequest(string queueUrl, string receiptHandle)
{
    public string QueueUrl { get; } = queueUrl;
    public string ReceiptHandle { get; } = receiptHandle;
}