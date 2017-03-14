using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        private readonly IAwsClientFactoryProxy _awsClientFactory;
        private readonly IRegionResourceCache<SqsQueueByName> _queueCache = new RegionResourceCache<SqsQueueByName>();

        public AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public SqsQueueByName EnsureTopicExistsWithQueueSubscribed(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var queue = EnsureQueueExists(region, queueConfig);
            if (TopicExistsInAnotherAccount(queueConfig))
            {
                var sqsClient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
                var snsClient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);
                var arnProvider = new ForeignTopicArnProvider(regionEndpoint, queueConfig.TopicSourceAccount, queueConfig.PublishEndpoint);
                snsClient.SubscribeQueue(arnProvider.GetArn(), sqsClient, queue.Url);
            }
            else
            {
                var snsclient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);
                var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);
                eventTopic.Create();

                EnsureQueueIsSubscribedToTopic(regionEndpoint, eventTopic, queue);

                var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
                SqsPolicy.Save(eventTopic.Arn, queue.Arn, queue.Url, sqsclient);
            }

            return queue;
        }

        private static bool TopicExistsInAnotherAccount(SqsReadConfiguration queueConfig)
        {
            return !string.IsNullOrWhiteSpace(queueConfig.TopicSourceAccount);
        }

        public SqsQueueByName EnsureQueueExists(string region, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var queue = _queueCache.TryGetFromCache(region, queueConfig.QueueName);
            if (queue != null)
            {
                return queue;
            }
            queue = new SqsQueueByName(regionEndpoint, queueConfig.QueueName, sqsclient, queueConfig.RetryCountBeforeSendingToErrorQueue);
            queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdated(queueConfig);

            _queueCache.AddToCache(region, queue.QueueName, queue);
            return queue;
        }

        private bool EnsureQueueIsSubscribedToTopic(RegionEndpoint region, SnsTopicByName eventTopic, SqsQueueByName queue)
        {
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(region);
            return eventTopic.Subscribe(sqsclient, queue);
        }
    }
}
