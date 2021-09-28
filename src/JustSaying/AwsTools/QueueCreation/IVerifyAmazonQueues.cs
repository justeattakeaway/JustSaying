using System.Threading;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

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
