using System;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace JustEat.Simples.NotificationStack.Stack.Amazon
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        [Obsolete("Please use the other overload that takes SqsConfiguration as parameter.")]
        public SqsQueueByName VerifyOrCreateQueue(string region, IMessageSerialisationRegister serialisationRegister, string queueName, string topic, int messageRetentionSeconds, int visibilityTimeoutSeconds = 30, int? instancePosition = null)
        {
            return VerifyOrCreateQueue(region, serialisationRegister,
                new SqsConfiguration
                {
                    QueueName = queueName,
                    Topic = topic,
                    MessageRetentionSeconds = messageRetentionSeconds,
                    VisibilityTimeoutSeconds = visibilityTimeoutSeconds,
                    InstancePosition = instancePosition
                });
        }

        public SqsQueueByName VerifyOrCreateQueue(string region, IMessageSerialisationRegister serialisationRegister, SqsConfiguration queueConfig)
        {
            var sqsclient = AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.GetBySystemName(region));
            var snsclient = AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(region));

            var queue = new SqsQueueByName(queueConfig.QueueName, sqsclient);

            var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);

            if (!queue.Exists())
                queue.Create(queueConfig.MessageRetentionSeconds, 0, queueConfig.VisibilityTimeoutSeconds, queueConfig.ErrorQueueOptOut, queueConfig.RetryCountBeforeSendingToErrorQueue);

            if (!eventTopic.Exists())
                eventTopic.Create();

            if (!eventTopic.IsSubscribed(queue))
                eventTopic.Subscribe(queue);

            if (!queue.HasPermission(eventTopic))
                queue.AddPermission(eventTopic);

            return queue;
        }
    }
}