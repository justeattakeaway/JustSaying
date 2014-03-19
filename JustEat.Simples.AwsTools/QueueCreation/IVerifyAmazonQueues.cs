using System;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace JustEat.Simples.NotificationStack.AwsTools.QueueCreation
{
    public interface IVerifyAmazonQueues
    {
        [Obsolete("Please use the other overload that takes SqsConfiguration as parameter.")]
        SqsQueueByName VerifyOrCreateQueue(string region, IMessageSerialisationRegister serialisationRegister, string queueName, string topic, int messageRetentionSeconds, int visibilityTimeoutSeconds = 30, int? instancePosition = null);
        SqsQueueByName VerifyOrCreateQueue(string region, IMessageSerialisationRegister serialisationRegister, SqsConfiguration queueConfig);
    }
}