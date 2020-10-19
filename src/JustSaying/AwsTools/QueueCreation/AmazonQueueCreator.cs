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

                    var sqsDetails = new SqsPolicyDetails
                    {
                        SourceArn = eventTopic.Arn,
                        QueueArn = queue.Arn,
                        QueueUri = queue.Uri
                    };
                    await SqsPolicy
                        .SaveAsync(sqsDetails, sqsClient)
                        .ConfigureAwait(false);
                }
            }

            // This StartupTask is intentionally not awaited, as it will be run when the bus is started.
            return new QueueWithAsyncStartup<SqsQueueByName>(StartupTask(), queueWithStartup.Queue);
        }

        private static bool TopicExistsInAnotherAccount(SqsReadConfiguration queueConfig)
        {
            return !string.IsNullOrWhiteSpace(queueConfig.TopicSourceAccount);
        }

        public QueueWithAsyncStartup<SqsQueueByName> EnsureQueueExists(
            string region,
            SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsclient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);

            var queue = new SqsQueueByName(regionEndpoint,
                queueConfig.QueueName,
                sqsclient,
                queueConfig.RetryCountBeforeSendingToErrorQueue,
                _loggerFactory);

            var startupTask = queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(queueConfig);

            // This startupTask is intentionally not awaited, as it will be run when the bus is started.
            return new QueueWithAsyncStartup<SqsQueueByName>(startupTask, queue);
        }

        private static async Task SubscribeQueueAndApplyFilterPolicyAsync(
            IAmazonSimpleNotificationService amazonSimpleNotificationService,
            string topicArn,
            IAmazonSQS amazonSQS,
            Uri queueUrl,
            string filterPolicy)
        {
            if (amazonSimpleNotificationService == null) throw new ArgumentNullException(nameof(amazonSimpleNotificationService));
            if (amazonSQS == null) throw new ArgumentNullException(nameof(amazonSQS));
            if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));
            if (string.IsNullOrEmpty(topicArn)) throw new ArgumentException("topicArn cannot be null or empty.", nameof(topicArn));

            var subscriptionArn = await amazonSimpleNotificationService
                .SubscribeQueueAsync(topicArn, amazonSQS, queueUrl.AbsoluteUri)
                .ConfigureAwait(false);

            var actualFilterPolicy =
                string.IsNullOrWhiteSpace(filterPolicy) ? EmptyFilterPolicy : filterPolicy;
            await amazonSimpleNotificationService
                .SetSubscriptionAttributesAsync(subscriptionArn, "FilterPolicy", actualFilterPolicy)
                .ConfigureAwait(false);
        }
    }
}
