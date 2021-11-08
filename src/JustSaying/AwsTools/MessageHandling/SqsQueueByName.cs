using System.Net;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling;

[Obsolete("SqsQueueBase and related classes are not intended for general usage and may be removed in a future major release")]
public class SqsQueueByName : SqsQueueByNameBase
{
    private readonly int _retryCountBeforeSendingToErrorQueue;

    internal ErrorQueue ErrorQueue { get; }

    public SqsQueueByName(RegionEndpoint region, string queueName, IAmazonSQS client, int retryCountBeforeSendingToErrorQueue, ILoggerFactory loggerFactory)
        : base(region, queueName, client, loggerFactory)
    {
        _retryCountBeforeSendingToErrorQueue = retryCountBeforeSendingToErrorQueue;
        ErrorQueue = new ErrorQueue(region, queueName, client, loggerFactory);
    }

    public override async Task<bool> CreateAsync(SqsBasicConfiguration queueConfig, int attempt = 0, CancellationToken cancellationToken = default)
    {
        if (NeedErrorQueue(queueConfig))
        {
            var exists = await ErrorQueue.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                using (Logger.Time("Creating error queue {QueueName}", ErrorQueue.QueueName))
                {
                    await ErrorQueue.CreateAsync(new SqsBasicConfiguration
                        {
                            ErrorQueueRetentionPeriod = queueConfig.ErrorQueueRetentionPeriod,
                            ErrorQueueOptOut = true
                        },
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                Logger.LogInformation("Error queue {QueueName} already exists, skipping", ErrorQueue.QueueName);
            }
        }

        using (Logger.Time("Creating queue {QueueName} attempt number {AttemptNumber}",
                   queueConfig.QueueName,
                   attempt))
        {
            return await base.CreateAsync(queueConfig, attempt, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool NeedErrorQueue(SqsBasicConfiguration queueConfig)
    {
        return !queueConfig.ErrorQueueOptOut;
    }

    public override async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (ErrorQueue != null)
        {
            await ErrorQueue.DeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        await base.DeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task UpdateRedrivePolicyAsync(RedrivePolicy requestedRedrivePolicy)
    {
        if (RedrivePolicyNeedsUpdating(requestedRedrivePolicy))
        {
            var request = new SetQueueAttributesRequest
            {
                QueueUrl = Uri.AbsoluteUri,
                Attributes = new Dictionary<string, string>
                {
                    {JustSayingConstants.AttributeRedrivePolicy, requestedRedrivePolicy.ToString()}
                }
            };

            var response = await Client.SetQueueAttributesAsync(request).ConfigureAwait(false);

            if (response?.HttpStatusCode == HttpStatusCode.OK)
            {
                RedrivePolicy = requestedRedrivePolicy;
            }
        }
    }

    public async Task EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(SqsReadConfiguration queueConfig, CancellationToken cancellationToken)
    {
        if (queueConfig == null) throw new ArgumentNullException(nameof(queueConfig));

        var exists = await ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            await CreateAsync(queueConfig, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await UpdateQueueAttributeAsync(queueConfig, cancellationToken).ConfigureAwait(false);
        }

        await ApplyTagsAsync(this, queueConfig.Tags, cancellationToken).ConfigureAwait(false);

        //Create an error queue for existing queues if they don't already have one
        if (ErrorQueue != null && NeedErrorQueue(queueConfig))
        {
            var errorQueueConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
            {
                ErrorQueueRetentionPeriod = queueConfig.ErrorQueueRetentionPeriod,
                ErrorQueueOptOut = true
            };

            var errorQueueExists = await ErrorQueue.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!errorQueueExists)
            {
                await ErrorQueue.CreateAsync(errorQueueConfig, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ErrorQueue.UpdateQueueAttributeAsync(errorQueueConfig, cancellationToken).ConfigureAwait(false);
            }

            await UpdateRedrivePolicyAsync(
                new RedrivePolicy(queueConfig.RetryCountBeforeSendingToErrorQueue, ErrorQueue.Arn)).ConfigureAwait(false);

            await ApplyTagsAsync(ErrorQueue, queueConfig.Tags, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ApplyTagsAsync(ISqsQueue queue, Dictionary<string, string> tags, CancellationToken cancellationToken)
    {
        if (tags == null || !tags.Any())
        {
            return;
        }

        await queue.TagQueueAsync(queue.Uri.ToString(), tags, cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("Added {TagCount} tags to queue {QueueName}",
            tags.Count, QueueName);
    }

    protected override Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig)
    {
        var policy = new Dictionary<string, string>
        {
            { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD ,queueConfig.MessageRetention.AsSecondsString() },
            { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , queueConfig.VisibilityTimeout.AsSecondsString() },
            { SQSConstants.ATTRIBUTE_DELAY_SECONDS  , queueConfig.DeliveryDelay.AsSecondsString() },
        };

        if (NeedErrorQueue(queueConfig))
        {
            policy.Add(JustSayingConstants.AttributeRedrivePolicy, new RedrivePolicy(_retryCountBeforeSendingToErrorQueue, ErrorQueue.Arn).ToString());
        }

        if (queueConfig.ServerSideEncryption != null)
        {
            policy.Add(JustSayingConstants.AttributeEncryptionKeyId, queueConfig.ServerSideEncryption.KmsMasterKeyId);
            policy.Add(JustSayingConstants.AttributeEncryptionKeyReusePeriodSecondId, queueConfig.ServerSideEncryption.KmsDataKeyReusePeriodSeconds);
        }

        return policy;
    }

    private bool RedrivePolicyNeedsUpdating(RedrivePolicy requestedRedrivePolicy)
        => RedrivePolicy == null || RedrivePolicy.MaximumReceives != requestedRedrivePolicy.MaximumReceives;
}