using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    [Obsolete("SqsQueueBase and related classes are not intended for general usage and may be removed in a future major release")]
    public class SqsQueueByName : SqsQueueByNameBase
    {
        private readonly int _retryCountBeforeSendingToErrorQueue;

        private readonly ErrorQueue _errorQueue;

        public SqsQueueByName(RegionEndpoint region, string queueName, IAmazonSQS client, int retryCountBeforeSendingToErrorQueue, ILoggerFactory loggerFactory)
            : base(region, queueName, client, loggerFactory)
        {
            _retryCountBeforeSendingToErrorQueue = retryCountBeforeSendingToErrorQueue;
            _errorQueue = new ErrorQueue(region, queueName, client, loggerFactory);
        }

        public override async Task<bool> CreateAsync(SqsBasicConfiguration queueConfig, int attempt = 0)
        {
            if (NeedErrorQueue(queueConfig))
            {
                var exists = await _errorQueue.ExistsAsync().ConfigureAwait(false);
                if (!exists)
                {
                    using (Logger.Time("Creating error queue {QueueName}", _errorQueue.QueueName))
                    {
                        await _errorQueue.CreateAsync(new SqsBasicConfiguration
                        {
                            ErrorQueueRetentionPeriod = queueConfig.ErrorQueueRetentionPeriod,
                            ErrorQueueOptOut = true
                        }).ConfigureAwait(false);
                    }
                }
                else
                {
                    Logger.LogInformation("Error queue {QueueName} already exists, skipping", _errorQueue.QueueName);
                }
            }

            using (Logger.Time("Creating queue {QueueName} attempt number {AttemptNumber}",
                queueConfig.QueueName,
                attempt))
            {
                return await base.CreateAsync(queueConfig, attempt).ConfigureAwait(false);
            }
        }

        private static bool NeedErrorQueue(SqsBasicConfiguration queueConfig)
        {
            return !queueConfig.ErrorQueueOptOut;
        }

        public override async Task DeleteAsync()
        {
            if (_errorQueue != null)
            {
                await _errorQueue.DeleteAsync().ConfigureAwait(false);
            }

            await base.DeleteAsync().ConfigureAwait(false);
        }

        private async Task UpdateRedrivePolicyAsync(RedrivePolicy requestedRedrivePolicy)
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

        public async Task EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(SqsReadConfiguration queueConfig)
        {
            if (queueConfig == null) throw new ArgumentNullException(nameof(queueConfig));

            var exists = await ExistsAsync().ConfigureAwait(false);
            if (!exists)
            {
                await CreateAsync(queueConfig).ConfigureAwait(false);
            }
            else
            {
                await UpdateQueueAttributeAsync(queueConfig).ConfigureAwait(false);
            }

            await ApplyTagsAsync(this, queueConfig.Tags).ConfigureAwait(false);

            //Create an error queue for existing queues if they don't already have one
            if (_errorQueue != null && NeedErrorQueue(queueConfig))
            {
                var errorQueueConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
                {
                    ErrorQueueRetentionPeriod = queueConfig.ErrorQueueRetentionPeriod,
                    ErrorQueueOptOut = true
                };

                var errorQueueExists = await _errorQueue.ExistsAsync().ConfigureAwait(false);
                if (!errorQueueExists)
                {
                    await _errorQueue.CreateAsync(errorQueueConfig).ConfigureAwait(false);
                }
                else
                {
                    await _errorQueue.UpdateQueueAttributeAsync(errorQueueConfig).ConfigureAwait(false);
                }

                await UpdateRedrivePolicyAsync(
                    new RedrivePolicy(queueConfig.RetryCountBeforeSendingToErrorQueue, _errorQueue.Arn)).ConfigureAwait(false);

                await ApplyTagsAsync(_errorQueue, queueConfig.Tags).ConfigureAwait(false);
            }
        }

        private async Task ApplyTagsAsync(ISqsQueue queue, Dictionary<string, string> tags)
        {
            if (tags == null || !tags.Any())
            {
                return;
            }

            var tagRequest = new TagQueueRequest
            {
                QueueUrl = queue.Uri.ToString(),
                Tags = tags
            };

            await queue.Client.TagQueueAsync(tagRequest).ConfigureAwait(false);

            Logger.LogInformation("Added {TagCount} tags to queue {QueueName}", tagRequest.Tags.Count, QueueName);
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
                policy.Add(JustSayingConstants.AttributeRedrivePolicy, new RedrivePolicy(_retryCountBeforeSendingToErrorQueue, _errorQueue.Arn).ToString());
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
}
