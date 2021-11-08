namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class FakeTagQueueRequest
    {
        public FakeTagQueueRequest(string queueUrl, Dictionary<string, string> tags)
        {
            QueueUrl = queueUrl;
            Tags = tags;
        }

        public string QueueUrl { get; }
        public Dictionary<string, string> Tags { get; }
    }
}
