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
public class SqsQueueByName(
    RegionEndpoint region,
    string queueName,
    IAmazonSQS client,
    int retryCountBeforeSendingToErrorQueue,
    ILoggerFactory loggerFactory) : SqsQueueByNameBase(region, queueName, client, loggerFactory)
{
    internal ErrorQueue ErrorQueue { get; } = new ErrorQueue(region, queueName, client, loggerFactory);

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
                            ServerSideEncryption = queueConfig.ServerSideEncryption,
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

    public Task EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(SqsReadConfiguration queueConfig, CancellationToken cancellationToken)
    {
        if (queueConfig == null) throw new ArgumentNullException(nameof(queueConfig));

        return EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(queueConfig, queueConfig.Tags, cancellationToken);
    }

    internal async Task EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(
        SqsBasicConfiguration queueConfig,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken)
    {
        var exists = await ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            await CreateAsync(queueConfig, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await UpdateQueueAttributeAsync(queueConfig, cancellationToken).ConfigureAwait(false);
        }

        await ApplyTagsAsync(this, tags, cancellationToken).ConfigureAwait(false);

        //Create an error queue for existing queues if they don't already have one
        if (ErrorQueue != null && NeedErrorQueue(queueConfig))
        {
            var errorQueueConfig = new SqsBasicConfiguration
            {
                ErrorQueueRetentionPeriod = queueConfig.ErrorQueueRetentionPeriod,
                ServerSideEncryption = queueConfig.ServerSideEncryption,
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

            await ApplyTagsAsync(ErrorQueue, tags, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads the settings of the queue as it exists now, or returns <see langword="null"/> when it
    /// doesn't exist. A registration that declares only some of the queue's settings (a publication)
    /// overlays its declared settings on these, so it converges what it declares and leaves the rest
    /// alone, rather than resetting them to the defaults.
    /// </summary>
    internal async Task<SqsBasicConfiguration> GetCurrentConfigurationAsync(CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var configuration = new SqsBasicConfiguration
        {
            QueueName = QueueName,
            MessageRetention = MessageRetentionPeriod,
            VisibilityTimeout = VisibilityTimeout,
            DeliveryDelay = DeliveryDelay,
            ServerSideEncryption = ServerSideEncryption,
            ErrorQueueOptOut = RedrivePolicy is null,
        };

        if (RedrivePolicy is not null)
        {
            configuration.RetryCountBeforeSendingToErrorQueue = RedrivePolicy.MaximumReceives;

            if (await ErrorQueue.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                configuration.ErrorQueueRetentionPeriod = ErrorQueue.MessageRetentionPeriod;
            }
        }

        return configuration;
    }

    private async Task ApplyTagsAsync(ISqsQueue queue, Dictionary<string, string> tags, CancellationToken cancellationToken)
    {
        if (tags == null || tags.Count == 0)
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
            policy.Add(JustSayingConstants.AttributeRedrivePolicy, new RedrivePolicy(retryCountBeforeSendingToErrorQueue, ErrorQueue.Arn).ToString());
        }

        if (queueConfig.ServerSideEncryption != null)
        {
            policy.Add(JustSayingConstants.AttributeEncryptionKeyId, queueConfig.ServerSideEncryption.KmsMasterKeyId);
            policy.Add(JustSayingConstants.AttributeEncryptionKeyReusePeriodSecondId, queueConfig.ServerSideEncryption.KmsDataKeyReusePeriod.AsSecondsString());
        }

        return policy;
    }

    private bool RedrivePolicyNeedsUpdating(RedrivePolicy requestedRedrivePolicy)
        => RedrivePolicy == null || RedrivePolicy.MaximumReceives != requestedRedrivePolicy.MaximumReceives;
}
