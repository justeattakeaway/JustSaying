using System;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        [Obsolete("Please use the other overload that takes SqsConfiguration as parameter.")]
        SqsQueueByName VerifyOrCreateQueue(string region, IMessageSerialisationRegister serialisationRegister, string queueName, string topic, int messageRetentionSeconds, int visibilityTimeoutSeconds = 30, int? instancePosition = null);
        SqsQueueByName VerifyOrCreateQueue(string region, IMessageSerialisationRegister serialisationRegister, SqsConfiguration queueConfig);
    }
}