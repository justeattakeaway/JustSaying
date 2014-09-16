using System;
using Amazon;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
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

            var queue = new SqsQueueByName(queueConfig.QueueName, sqsclient, queueConfig.RetryCountBeforeSendingToErrorQueue);

            var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);

            queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdated(queueConfig);
            
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