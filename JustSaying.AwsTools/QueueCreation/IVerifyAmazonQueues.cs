using System;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        SqsQueueByName EnsureTopicExistsWithQueueSubscribed(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig);
        SqsQueueByName EnsureQueueExists(string region, SqsReadConfiguration queueConfig);
    }
}