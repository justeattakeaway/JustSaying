using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
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
            var sqsClient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var snsClient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);

            var queue = await EnsureQueueExistsAsync(region, queueConfig);

            if (TopicExistsInAnotherAccount(queueConfig))
            {
                var arnProvider = new ForeignTopicArnProvider(regionEndpoint, queueConfig.TopicSourceAccount, queueConfig.PublishEndpoint);

                await snsClient.SubscribeQueueAsync(arnProvider.GetArn(), sqsClient, queue.Url);
            }
            else
            {
                var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsClient, serialisationRegister, _loggerFactory);
                eventTopic.Create();

                await EnsureQueueIsSubscribedToTopic(eventTopic, queue);

                await SqsPolicy.SaveAsync(eventTopic.Arn, queue.Arn, queue.Url, sqsClient);
            }

            return queue;
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

        private async Task<bool> EnsureQueueIsSubscribedToTopic(SnsTopicByName eventTopic, SqsQueueByName queue)
        {
            return await eventTopic.SubscribeAsync(queue);
        }
    }
}
