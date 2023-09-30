namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class FakeTagQueueRequest(string queueUrl, Dictionary<string, string> tags)
{
    public string QueueUrl { get; } = queueUrl;
    public Dictionary<string, string> Tags { get; } = tags;
}