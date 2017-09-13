using System.Threading.Tasks;
using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        private readonly IAwsClientFactoryProxy _awsClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRegionResourceCache<SqsQueueByName> _queueCache = new RegionResourceCache<SqsQueueByName>();
        private readonly IRegionResourceCache<SnsTopicByName> _topicCache = new RegionResourceCache<SnsTopicByName>();

        public AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory, ILoggerFactory loggerFactory)
        {
            _awsClientFactory = awsClientFactory;
            _loggerFactory = loggerFactory;
        }
        
        public async Task<SqsQueueByName> EnsureTopicExistsWithQueueSubscribedAsync(string region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var queue = await EnsureQueueExistsAsync(region, queueConfig);
            if (TopicExistsInAnotherAccount(queueConfig))
            {
                var sqsClient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
                var snsClient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);
                var arnProvider = new ForeignTopicArnProvider(regionEndpoint, queueConfig.TopicSourceAccount, queueConfig.PublishEndpoint);

                await snsClient.SubscribeQueueAsync(await arnProvider.GetArnAsync(), sqsClient, queue.Url);
            }
            else
            {
                var eventTopic = await EnsureTopicExists(regionEndpoint, serialisationRegister, queueConfig);
                await EnsureQueueIsSubscribedToTopic(regionEndpoint, eventTopic, queue);

                var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
                await SqsPolicy.SaveAsync(eventTopic.Arn, queue.Arn, queue.Url, sqsclient);
            }

            return queue;
        }

        private static bool TopicExistsInAnotherAccount(SqsReadConfiguration queueConfig) =>
            !string.IsNullOrWhiteSpace(queueConfig.TopicSourceAccount);
   
        public async Task<SqsQueueByName> EnsureQueueExistsAsync(string region, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var queue = _queueCache.TryGetFromCache(region, queueConfig.QueueName);
            if (queue != null)
            {
                return queue;
            }
            queue = new SqsQueueByName(regionEndpoint, queueConfig.QueueName, sqsclient, queueConfig.RetryCountBeforeSendingToErrorQueue, _loggerFactory);
            await queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(queueConfig);

            _queueCache.AddToCache(region, queue.QueueName, queue);
            return queue;
        }

        private async Task<SnsTopicByName> EnsureTopicExists(RegionEndpoint region, IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            var snsclient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(region);

            var eventTopic = _topicCache.TryGetFromCache(region.SystemName, queueConfig.PublishEndpoint);
            if (eventTopic != null)
                return eventTopic;

            eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsclient, serialisationRegister, _loggerFactory);
            _topicCache.AddToCache(region.SystemName, queueConfig.PublishEndpoint, eventTopic);

            var exists = await eventTopic.ExistsAsync();

            if (!exists)
            {
                await eventTopic.CreateAsync();
            }

            return eventTopic;
        }

        private async Task EnsureQueueIsSubscribedToTopic(RegionEndpoint region, SnsTopicByName eventTopic, SqsQueueByName queue)
        {
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(region);
            await eventTopic.SubscribeAsync(sqsclient, queue);
        }
    }
}
