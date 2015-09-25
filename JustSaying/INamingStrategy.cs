namespace JustSaying
{
    public interface INamingStrategy
    {
        string GetTopicName(string topicName, string messageType);
        string GetQueueName(string queueName, string messageType);
    }
}