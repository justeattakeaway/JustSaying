namespace JustSaying.v2.Configuration
{
    public interface IAwsTopicSubscriberConfiguration : IAwsSubscriberConfiguration
    {
        string TopicNameOverride { get; set; }
        string TopicSourceAccount { get; set; }
    }

    public class AwsTopicSubscriberConfiguration : AwsSubscriberConfiguration, IAwsTopicSubscriberConfiguration
    {
        public string TopicNameOverride { get; set; }
        public string TopicSourceAccount { get; set; }
    }
}