using JustSaying.Messaging;

namespace JustSaying.AwsTools.Publishing
{
    public interface IMessagePublisherFactory
    {
        IMessagePublisher CreateSnsPublisher(string topicName, bool throwOnPublishFailure = false);

        IMessagePublisher CreateSqsPublisher(string queueName);

    }
}
