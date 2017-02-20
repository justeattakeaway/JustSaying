using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
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
        private bool _disableTopicCheckOnSubscribe;

        public AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory, ILoggerFactory loggerFactory)
        {
            _awsClientFactory = awsClientFactory;
            _loggerFactory = loggerFactory;
        }

        public SqsQueueByName EnsureTopicExistsWithQueueSubscribed(string region,
            IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig)
        {
            return EnsureTopicExistsWithQueueSubscribedAsync(region, serialisationRegister, queueConfig)
                  .GetAwaiter().GetResult();
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

                await snsClient.SubscribeQueueAsync(arnProvider.GetArn(), sqsClient, queue.Url);
            }
            else
            {
                var eventTopic = _disableTopicCheckOnSubscribe
                    ? CreateTopicWithoutCheckingForExistence(serialisationRegister, queueConfig, regionEndpoint)
                    : await EnsureTopicExists(regionEndpoint, serialisationRegister, queueConfig);

                await EnsureQueueIsSubscribedToTopic(regionEndpoint, eventTopic, queue);

                var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
                await SqsPolicy.SaveAsync(eventTopic.Arn, queue.Arn, queue.Url, sqsclient);
            }

            return queue;
        }

        private SnsTopicByName CreateTopicWithoutCheckingForExistence(IMessageSerialisationRegister serialisationRegister, SqsReadConfiguration queueConfig, RegionEndpoint regionEndpoint)
        {
            return new SnsTopicByName(queueConfig.PublishEndpoint, _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint), serialisationRegister, _loggerFactory);
        }

        private static bool TopicExistsInAnotherAccount(SqsReadConfiguration queueConfig)
        {
            return !string.IsNullOrWhiteSpace(queueConfig.TopicSourceAccount);
        }

        public SqsQueueByName EnsureQueueExists(string region, SqsReadConfiguration queueConfig)
        {
            return EnsureQueueExistsAsync(region, queueConfig)
                .GetAwaiter().GetResult();
        }

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

        public async Task PreLoadTopicCache(string region, IMessageSerialisationRegister serialisationRegister)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var snsclient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);
            var topics = await ListTopics(snsclient);

            foreach (var topic in topics)
            {
                var eventTopic = new SnsTopicByName(topic, snsclient, serialisationRegister, _loggerFactory);
                _topicCache.AddToCache(regionEndpoint.SystemName, eventTopic.TopicName, eventTopic);
            }
        }

        public void DisableTopicCheckOnSubscribe()
        {
            _disableTopicCheckOnSubscribe = true;
        }

        private static async Task<List<Topic>> ListTopics(IAmazonSimpleNotificationService snsclient)
        {
            var topics = new List<Topic>();
            string nextToken = null;
            do
            {
                var listTopicsResponse = await snsclient.ListTopicsAsync(new ListTopicsRequest
                {
                    NextToken = nextToken
                });
                if (listTopicsResponse?.Topics == null || listTopicsResponse.Topics.Count == 0)
                {
                    break;
                }
                topics.AddRange(listTopicsResponse.Topics);
                nextToken = listTopicsResponse.NextToken;
            } while (!string.IsNullOrEmpty(nextToken));
            return topics;

        }
    }
}
