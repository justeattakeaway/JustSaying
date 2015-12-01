using System;
using Amazon;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        public SqsQueueByName EnsureTopicExistsWithQueueSubscribed(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var queue = EnsureQueueExists(region, queueConfig);
            var eventTopic = EnsureTopicExists(region, serialisationRegister, queueConfig);
            EnsureQueueIsSubscribedToTopic(region, eventTopic, queue);

            return queue;
        }

        public SqsQueueByName EnsureQueueExists(string region, SqsReadConfiguration queueConfig)
        {
            var sqsclient = SqsClientFactory.Create(RegionEndpoint.GetBySystemName(region));
            var queue = new SqsQueueByName(queueConfig.QueueName, sqsclient, queueConfig.RetryCountBeforeSendingToErrorQueue);
            queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdated(queueConfig);
            return queue;
        }

        private static SnsTopicByName EnsureTopicExists(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var snsclient = AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(region));
            var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);

            if (!eventTopic.Exists())
                eventTopic.Create();

            return eventTopic;
        }

        private static void EnsureQueueIsSubscribedToTopic(string region, SnsTopicByName eventTopic, SqsQueueByName queue)
        {
            var sqsclient = SqsClientFactory.Create(RegionEndpoint.GetBySystemName(region));
            eventTopic.Subscribe(sqsclient, queue);
        }
    }
}