using JustSaying.v2.Configuration;

namespace JustSaying.v2
{
    public interface IAwsNamingStrategy
    {
        string GetTopicName<T>(IAwsTopicNameConfiguration configuration);
        string GetQueueName<T>(IAwsQueueNameConfiguration configuration, bool topicSubscription);
    }

    public class AwsNamingStrategy : IAwsNamingStrategy
    {
        public string GetTopicName<T>(IAwsTopicNameConfiguration configuration) 
            => string.IsNullOrEmpty(configuration.TopicNameOverride) ? typeof(T).Name.ToLowerInvariant() : configuration.TopicNameOverride;

        public string GetQueueName<T>(IAwsQueueNameConfiguration configuration, bool topicSubscription)
            => string.IsNullOrEmpty(configuration.QueueNameOverride) ? typeof(T).Name.ToLowerInvariant() : configuration.QueueNameOverride;
    }
}