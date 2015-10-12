using JustSaying.AwsTools.QueueCreation;

namespace JustSaying
{
    public interface INamingStrategy
    {
        string GetTopicName(string topicName, string messageType);
        string GetQueueName(SqsReadConfiguration sqsConfig, string messageType);
    }
}