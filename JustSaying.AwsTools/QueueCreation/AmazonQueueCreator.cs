using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        private readonly IAwsClientFactoryProxy _awsClientFactory;
        private readonly IRegionResourceCache<SqsQueueByName> _queueCache = new RegionResourceCache<SqsQueueByName>();
        private readonly IRegionResourceCache<SnsTopicByName> _topicCache = new RegionResourceCache<SnsTopicByName>();

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

                // todo make async
                snsClient.SubscribeQueueAsync(arnProvider.GetArn(), sqsClient, queue.Url)
                    .GetAwaiter().GetResult();
            }
            else
            {
                var eventTopic = EnsureTopicExists(regionEndpoint, serialisationRegister, queueConfig);
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

        private SnsTopicByName EnsureTopicExists(RegionEndpoint region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var snsclient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(region);

            var eventTopic = _topicCache.TryGetFromCache(region.SystemName, queueConfig.PublishEndpoint);
            if (eventTopic != null)
                return eventTopic;

            eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister);
            _topicCache.AddToCache(region.SystemName, queueConfig.PublishEndpoint, eventTopic);

            if (!eventTopic.Exists())
            {
                eventTopic.Create();
            }

            return eventTopic;
        }

        private void EnsureQueueIsSubscribedToTopic(RegionEndpoint region, SnsTopicByName eventTopic, SqsQueueByName queue)
        {
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(region);
            eventTopic.Subscribe(sqsclient, queue);
        }
    }
}