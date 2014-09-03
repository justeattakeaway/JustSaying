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
                new SqsReadConfiguration
                {
                    QueueName = queueName,
                    Topic = topic,
                    MessageRetentionSeconds = messageRetentionSeconds,
                    VisibilityTimeoutSeconds = visibilityTimeoutSeconds,
                    InstancePosition = instancePosition
                });
        }

        public SqsQueueByName VerifyOrCreateQueue(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var sqsclient = AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.GetBySystemName(region));
            var snsclient = AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(region));

            var queue = new SqsQueueByName(queueConfig.QueueName, sqsclient, queueConfig.RetryCountBeforeSendingToErrorQueue);

            var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);

            if (!queue.Exists())
                queue.Create(queueConfig.MessageRetentionSeconds, 0, queueConfig.VisibilityTimeoutSeconds, queueConfig.ErrorQueueOptOut, queueConfig.RetryCountBeforeSendingToErrorQueue);

            //Create an error queue for existing queues if they don't already have one
            if(queue.ErrorQueue != null && !queue.ErrorQueue.Exists())
                queue.ErrorQueue.Create(JustSayingConstants.MAXIMUM_RETENTION_PERIOD, JustSayingConstants.DEFAULT_CREATE_REATTEMPT, JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT, errorQueueOptOut: true);
            queue.UpdateRedrivePolicy(new RedrivePolicy(queueConfig.RetryCountBeforeSendingToErrorQueue, queue.ErrorQueue.Arn));

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