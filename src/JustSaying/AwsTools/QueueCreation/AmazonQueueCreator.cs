using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
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

        public QueueWithAsyncStartup EnsureTopicExistsWithQueueSubscribed(
            string region,
            IMessageSerializationRegister serializationRegister,
            SqsReadConfiguration queueConfig,
            IMessageSubjectProvider messageSubjectProvider,
            InfrastructureAction infrastructureAction)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsClient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
            var snsClient = _awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);

            var queueWithStartup = EnsureQueueExists(region, queueConfig);

            async Task StartupTask()
            {
                await queueWithStartup.StartupTask.Invoke().ConfigureAwait(false);
                var queue = queueWithStartup.Queue;
                if (infrastructureAction == InfrastructureAction.CreateIfMissing)
                {
                    if (TopicExistsInAnotherAccount(queueConfig))
                        await SubscribeToTopic(queueConfig, regionEndpoint, snsClient, sqsClient, queue);
                    else
                        await CreateAndSubscribeToTopic(serializationRegister, queueConfig, messageSubjectProvider, snsClient, sqsClient, queue);
                }
                else if (infrastructureAction == InfrastructureAction.ValidateExists)
                {
                    if (TopicExistsInAnotherAccount(queueConfig))
                        await CheckForeignTopicExists(queueConfig, regionEndpoint, snsClient);
                    else
                        await CheckTopicAndSubscriptioon(serializationRegister, queueConfig, messageSubjectProvider, snsClient, sqsClient, queue);
                }
            }

            return new QueueWithAsyncStartup(StartupTask, queueWithStartup.Queue);
        }

        private async Task CheckForeignTopicExists(SqsReadConfiguration queueConfig, RegionEndpoint regionEndpoint, IAmazonSimpleNotificationService snsClient)
        {
            var arnProvider = new ForeignTopicArnProvider(regionEndpoint,
                queueConfig.TopicSourceAccount,
                queueConfig.PublishEndpoint,
                snsClient);

            //TODO: Currently a no-op on this class - but we should check x-account - if we have a queue should we check for subscription instead
            var exists = await arnProvider.ArnExistsAsync();
            if (!exists)
                throw new InvalidOperationException($"The topic {queueConfig.PublishEndpoint} in account {queueConfig.TopicSourceAccount} does not exist");
        }


       private async Task CheckTopicAndSubscriptioon(IMessageSerializationRegister serializationRegister, SqsReadConfiguration queueConfig, IMessageSubjectProvider messageSubjectProvider, IAmazonSimpleNotificationService snsClient, IAmazonSQS sqsClient, ISqsQueue queue)
       {
#pragma warning disable 618
                var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint,
                snsClient,
                serializationRegister,
                _loggerFactory,
                messageSubjectProvider);
#pragma warning restore 618

           var topicExists = await eventTopic.ExistsAsync();
           if (!topicExists)
               throw new InvalidOperationException($"The topic {queueConfig.PublishEndpoint} does not exist");

           bool subExists = false;
           ListSubscriptionsByTopicResponse response;
           do
           {
               response = await snsClient.ListSubscriptionsByTopicAsync(new ListSubscriptionsByTopicRequest {TopicArn =eventTopic.Arn});
               subExists  = response.Subscriptions.Any(sub => (sub.Protocol.ToLower() == "sqs") && (sub.Endpoint == queue.Arn));
           } while (!subExists && response.NextToken != null);

           if (!subExists)
               throw new InvalidOperationException($"Could not find subscription on topic: {eventTopic.Arn} for queue: {queue.Arn} ");

       }

        private static async Task SubscribeToTopic(SqsReadConfiguration queueConfig, RegionEndpoint regionEndpoint, IAmazonSimpleNotificationService snsClient, IAmazonSQS sqsClient, ISqsQueue queue)
        {
            var arnProvider = new ForeignTopicArnProvider(regionEndpoint,
                queueConfig.TopicSourceAccount,
                queueConfig.PublishEndpoint,
                snsClient);

            var topicArn = await arnProvider.GetArnAsync().ConfigureAwait(false);
            await SubscribeQueueAndApplyFilterPolicyAsync(snsClient,
                topicArn,
                sqsClient,
                queue.Uri,
                queueConfig.FilterPolicy).ConfigureAwait(false);
        }

        private async Task CreateAndSubscribeToTopic(IMessageSerializationRegister serializationRegister, SqsReadConfiguration queueConfig, IMessageSubjectProvider messageSubjectProvider, IAmazonSimpleNotificationService snsClient, IAmazonSQS sqsClient, ISqsQueue queue)
        {
#pragma warning disable 618
            var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint,
                snsClient,
                serializationRegister,
                _loggerFactory,
                messageSubjectProvider);
#pragma warning restore 618
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


        private static bool TopicExistsInAnotherAccount(SqsReadConfiguration queueConfig)
        {
            return !string.IsNullOrWhiteSpace(queueConfig.TopicSourceAccount);
        }

        public QueueWithAsyncStartup EnsureQueueExists(
            string region,
            SqsReadConfiguration queueConfig)
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var sqsClient = _awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);

#pragma warning disable 618
            var queue = new SqsQueueByName(regionEndpoint,
                queueConfig.QueueName,
                sqsClient,
                queueConfig.RetryCountBeforeSendingToErrorQueue,
                _loggerFactory);
#pragma warning restore 618

            var startupTask = new Func<Task>(() => queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(queueConfig));

            return new QueueWithAsyncStartup(startupTask, queue);
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
            if (string.IsNullOrEmpty(topicArn)) throw new ArgumentException($"{nameof(topicArn)} cannot be null or empty.", nameof(topicArn));

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
