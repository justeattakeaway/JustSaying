using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.QueueCreation
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        private readonly IAwsClientFactoryProxy _awsClientFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly RegionResourceCache<SqsQueueByName> _queueCache = new RegionResourceCache<SqsQueueByName>();

        private const string EmptyFilterPolicy = "{}";

        public AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory, ILoggerFactory loggerFactory)
        {
            _awsClientFactory = awsClientFactory;
            _loggerFactory = loggerFactory;
        }

        public QueueWithAsyncStartup<SqsQueueByName> EnsureTopicExistsWithQueueSubscribed(
            string region,
            IMessageSerializationRegister serializationRegister,
            SqsReadConfiguration queueConfig,
            IMessageSubjectProvider messageSubjectProvider)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsClient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var snsClient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);

            var queueWithStartup = EnsureQueueExists(region, queueConfig);

            async Task StartupTask()
            {
                await queueWithStartup.StartupTask.ConfigureAwait(false);
                var queue = queueWithStartup.Queue;
                if (TopicExistsInAnotherAccount(queueConfig))
                {
                    var arnProvider = new ForeignTopicArnProvider(regionEndpoint,
                        queueConfig.TopicSourceAccount,
                        queueConfig.PublishEndpoint);

                    var topicArn = await arnProvider.GetArnAsync().ConfigureAwait(false);
                    await SubscribeQueueAndApplyFilterPolicyAsync(snsClient,
                        topicArn,
                        sqsClient,
                        queue.Uri,
                        queueConfig.FilterPolicy).ConfigureAwait(false);
                }
                else
                {
                    var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint,
                        snsClient,
                        serializationRegister,
                        _loggerFactory,
                        messageSubjectProvider);
                    await eventTopic.CreateAsync().ConfigureAwait(false);

                    await SubscribeQueueAndApplyFilterPolicyAsync(snsClient,
                        eventTopic.Arn,
                        sqsClient,
                        queue.Uri,
                        queueConfig.FilterPolicy).ConfigureAwait(false);

                    await SqsPolicy
                        .SaveAsync(eventTopic.Arn, queue.Arn, queue.Uri, sqsClient)
                        .ConfigureAwait(false);
                }
            }

            return new QueueWithAsyncStartup<SqsQueueByName>(StartupTask(), queueWithStartup.Queue);
        }

        private static bool TopicExistsInAnotherAccount(SqsReadConfiguration queueConfig)
        {
            return !string.IsNullOrWhiteSpace(queueConfig.TopicSourceAccount);
        }

        public QueueWithAsyncStartup<SqsQueueByName> EnsureQueueExists(string region, SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var queue = _queueCache.TryGetFromCache(region, queueConfig.QueueName);
            if (queue != null)
            {
                return new QueueWithAsyncStartup<SqsQueueByName>(queue);
            }
            queue = new SqsQueueByName(regionEndpoint, queueConfig.QueueName, sqsclient, queueConfig.RetryCountBeforeSendingToErrorQueue, _loggerFactory);

            _queueCache.AddToCache(region, queue.QueueName, queue);

            var startupTask = queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(queueConfig);

            return new QueueWithAsyncStartup<SqsQueueByName>(startupTask, queue);
        }

        private static async Task SubscribeQueueAndApplyFilterPolicyAsync(
            IAmazonSimpleNotificationService amazonSimpleNotificationService,
            string topicArn, IAmazonSQS amazonSQS, Uri queueUrl, string filterPolicy)
        {
            var subscriptionArn = await amazonSimpleNotificationService.SubscribeQueueAsync(topicArn, amazonSQS, queueUrl.AbsoluteUri)
                .ConfigureAwait(false);

            var actualFilterPolicy = string.IsNullOrWhiteSpace(filterPolicy) ? EmptyFilterPolicy : filterPolicy;
            await amazonSimpleNotificationService.SetSubscriptionAttributesAsync(subscriptionArn, "FilterPolicy", actualFilterPolicy).ConfigureAwait(false);
        }
    }
}
