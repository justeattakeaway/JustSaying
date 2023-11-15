namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeChangeMessageVisibilityRequest(string queueUrl, string receiptHandle, int visibilityTimeoutInSeconds)
{
    public string QueueUrl { get; } = queueUrl;
    public string ReceiptHandle { get; } = receiptHandle;
    public int VisibilityTimeoutInSeconds { get; } = visibilityTimeoutInSeconds;
}
