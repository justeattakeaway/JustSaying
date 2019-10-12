using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        Task<SqsQueueByName> EnsureTopicExistsWithQueueSubscribedAsync(
            string region,
            IMessageSerializationRegister serializationRegister,
            SqsReadConfiguration queueConfig,
            IMessageSubjectProvider messageSubjectProvider);

        Task<SqsQueueByName> EnsureQueueExistsAsync(string region, SqsReadConfiguration queueConfig);
    }
}
