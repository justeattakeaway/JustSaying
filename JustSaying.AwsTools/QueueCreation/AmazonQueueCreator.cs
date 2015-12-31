using System;
using Amazon;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        private readonly IAwsClientFactoryProxy awsClientFactory;

        public AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory)
        {
            this.awsClientFactory = awsClientFactory;
        }

        public SqsQueueByName EnsureTopicExistsWithQueueSubscribed(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var queue = EnsureQueueExists(region, queueConfig);
            var eventTopic = EnsureTopicExists(regionEndpoint, serialisationRegister, queueConfig);
            EnsureQueueIsSubscribedToTopic(regionEndpoint, eventTopic, queue);

            return queue;
        }

        public SqsQueueByName EnsureQueueExists(string region, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsclient = awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var queue = new SqsQueueByName(regionEndpoint, queueConfig.QueueName, sqsclient, queueConfig.RetryCountBeforeSendingToErrorQueue);
            queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdated(queueConfig);
            return queue;
        }

        private SnsTopicByName EnsureTopicExists(RegionEndpoint region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var snsclient = awsClientFactory.GetAwsClientFactory().GetSnsClient(region);
            var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);

            if (!eventTopic.Exists())
                eventTopic.Create();

            return eventTopic;
        }

        private void EnsureQueueIsSubscribedToTopic(RegionEndpoint region, SnsTopicByName eventTopic, SqsQueueByName queue)
        {
            var sqsclient = awsClientFactory.GetAwsClientFactory().GetSqsClient(region);
            eventTopic.Subscribe(sqsclient, queue);
        }
    }
}