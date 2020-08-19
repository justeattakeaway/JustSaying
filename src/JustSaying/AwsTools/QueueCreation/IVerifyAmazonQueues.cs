using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        QueueWithAsyncStartup<SqsQueueByName> EnsureTopicExistsWithQueueSubscribed(
            string region,
            IMessageSerializationRegister serializationRegister,
            SqsReadConfiguration queueConfig,
            IMessageSubjectProvider messageSubjectProvider);

        QueueWithAsyncStartup<SqsQueueByName> EnsureQueueExists(string region, SqsReadConfiguration queueConfig);
    }
}
