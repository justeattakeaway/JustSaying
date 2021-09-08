using System.Threading;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        QueueWithAsyncStartup EnsureTopicExistsWithQueueSubscribed(
            string region,
            IMessageSerializationRegister serializationRegister,
            SqsReadConfiguration queueConfig,
            IMessageSubjectProvider messageSubjectProvider);

        QueueWithAsyncStartup EnsureQueueExists(
            string region,
            SqsReadConfiguration queueConfig);
    }
}
