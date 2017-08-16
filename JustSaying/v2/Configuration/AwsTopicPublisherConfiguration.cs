using System.Collections.Generic;

namespace JustSaying.v2.Configuration
{
    public interface IAwsTopicPublisherConfiguration : IAwsTopicNameConfiguration
    {
        IEnumerable<string> AdditionalSubscribers { get; set; }
    }

    public interface IAwsTopicNameConfiguration
    {
        string TopicNameOverride { get; set; }
    }

    public class AwsTopicPublisherConfiguration : IAwsTopicPublisherConfiguration
    {
        public IEnumerable<string> AdditionalSubscribers { get; set; }
        public string TopicNameOverride { get; set; }
    }
}