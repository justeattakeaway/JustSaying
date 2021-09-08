using System.Collections.Generic;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class TagQueueRequest
    {
        public TagQueueRequest(string queueUrl, Dictionary<string, string> tags)
        {
            QueueUrl = queueUrl;
            Tags = tags;
        }

        public string QueueUrl { get; }
        public Dictionary<string, string> Tags { get; }
    }
}
