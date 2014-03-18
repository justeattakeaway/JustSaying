using System;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace JustEat.Simples.NotificationStack.Stack.Amazon
{
    public interface IVerifyAmazonQueues
    {
        [Obsolete("Please use the other overload that takes SqsConfiguration as parameter.")]
        SqsQueueByName VerifyOrCreateQueue(IMessagingConfig configuration, IMessageSerialisationRegister serialisationRegister, string queueName, string topic, int messageRetentionSeconds, int visibilityTimeoutSeconds = 30, int? instancePosition = null);
        SqsQueueByName VerifyOrCreateQueue(IMessagingConfig configuration, IMessageSerialisationRegister serialisationRegister, SqsConfiguration queueConfig);
    }
}