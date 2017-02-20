using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        SqsQueueByName EnsureTopicExistsWithQueueSubscribed(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig);
        SnsTopicByName EnsureTopicExists(string region, IMessageSerialisationRegister serialisationRegister, string topicName);
        SqsQueueByName EnsureQueueExists(string region, SqsReadConfiguration queueConfig);
        Task PreLoadTopicCache(string region, IMessageSerialisationRegister busSerialisationRegister);
        void DisableTopicCheckOnSubscribe();
    }
}
