using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.QueueCreation;

public class AmazonQueueCreator(IAwsClientFactoryProxy awsClientFactory, ILoggerFactory loggerFactory) : IVerifyAmazonQueues
{
    public QueueWithAsyncStartup EnsureTopicExistsWithQueueSubscribed(
        string region,
        SqsReadConfiguration queueConfig)
    {
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        var sqsClient = awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);
        var snsClient = awsClientFactory.GetAwsClientFactory().GetSnsClient(regionEndpoint);

        var queueWithStartup = EnsureQueueExists(region, queueConfig);

        async Task StartupTask(CancellationToken cancellationToken)
        {
            await queueWithStartup.StartupTask.Invoke(cancellationToken).ConfigureAwait(false);
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
#pragma warning disable 618
                var eventTopic = new SnsTopicByName(queueConfig.PublishEndpoint, snsClient, loggerFactory);
#pragma warning restore 618
                await eventTopic.CreateAsync(cancellationToken).ConfigureAwait(false);

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

        return new QueueWithAsyncStartup(StartupTask, queueWithStartup.Queue);
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
        var sqsClient = awsClientFactory.GetAwsClientFactory().GetSqsClient(regionEndpoint);

#pragma warning disable 618
        var queue = new SqsQueueByName(regionEndpoint,
            queueConfig.QueueName,
            sqsClient,
            queueConfig.RetryCountBeforeSendingToErrorQueue,
            loggerFactory);
#pragma warning restore 618

        var startupTask = new Func<CancellationToken, Task>(ct => queue.EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(
            queueConfig, ct));

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
            string.IsNullOrWhiteSpace(filterPolicy) ? "{}" : filterPolicy;
        await amazonSimpleNotificationService
            .SetSubscriptionAttributesAsync(subscriptionArn, "FilterPolicy", actualFilterPolicy)
            .ConfigureAwait(false);
    }
}
