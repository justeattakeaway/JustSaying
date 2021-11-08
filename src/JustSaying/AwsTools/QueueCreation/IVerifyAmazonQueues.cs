using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        QueueWithAsyncStartup EnsureTopicExistsWithQueueSubscribed(
            string region,
            SqsReadConfiguration queueConfig);

        QueueWithAsyncStartup EnsureQueueExists(string region, SqsReadConfiguration queueConfig);
    }
}
