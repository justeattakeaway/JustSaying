namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeChangeMessageVisbilityRequest
{
    public FakeChangeMessageVisbilityRequest(string queueUrl, string receiptHandle, int visibilityTimeoutInSeconds)
    {
        QueueUrl = queueUrl;
        ReceiptHandle = receiptHandle;
        VisibilityTimeoutInSeconds = visibilityTimeoutInSeconds;
    }

    public string QueueUrl { get; }
    public string ReceiptHandle { get; }
    public int VisibilityTimeoutInSeconds { get; }
}