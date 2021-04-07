using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        QueueWithAsyncStartup EnsureTopicExistsWithQueueSubscribed(
            string region,
            IMessageSerializationRegister serializationRegister,
            SqsReadConfiguration queueConfig,
            IMessageSubjectProvider messageSubjectProvider,
            InfrastructureAction infrastructureAction);

        QueueWithAsyncStartup EnsureQueueExists(string region, SqsReadConfiguration queueConfig);
    }
}
