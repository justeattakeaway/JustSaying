using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using JustSaying.Messaging.Interrogation;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    [Obsolete("SqsQueueBase and related classes are not intended for general usage and may be removed in a future major release")]
    public abstract class SqsQueueByNameBase : ISqsQueue
    {
        protected SqsQueueByNameBase(RegionEndpoint region, string queueName, IAmazonSQS client, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            QueueName = queueName;
            Client = client;
            _region = region;
            Logger = loggerFactory.CreateLogger("JustSaying");
        }

        private readonly RegionEndpoint _region;
        private TimeSpan _visibilityTimeout;

        public string Arn { get; private set; }
        public Uri Uri { get; private set; }
        public IAmazonSQS Client { get; }
        public string QueueName { get; }
        internal TimeSpan MessageRetentionPeriod { get; set; }
        internal RedrivePolicy RedrivePolicy { get; set; }
        public string RegionSystemName => _region.SystemName;
        internal TimeSpan DeliveryDelay { get; private set; }
        internal ServerSideEncryption ServerSideEncryption { get; private set; }
        internal string Policy { get; private set; }
        protected ILogger Logger { get; }

        public virtual async Task<bool> ExistsAsync()
        {
            if (string.IsNullOrWhiteSpace(QueueName))
            {
                return false;
            }

            GetQueueUrlResponse result;

            try
            {
                using (Logger.Time(LogLevel.Debug, "Checking if queue '{QueueName}' exists", QueueName))
                {
                    result = await Client.GetQueueUrlAsync(QueueName).ConfigureAwait(false);
                }
            }
            catch (QueueDoesNotExistException)
            {
                return false;
            }

            if (result?.QueueUrl == null)
            {
                return false;
            }

            Uri = new Uri(result.QueueUrl);

            await SetQueuePropertiesAsync().ConfigureAwait(false);
            return true;
        }

        private static readonly TimeSpan CreateRetryDelay = TimeSpan.FromMinutes(1);

        public virtual async Task<bool> CreateAsync(SqsBasicConfiguration queueConfig, int attempt = 0)
        {
            // If we're on a delete timeout, throw after 3 attempts.
            const int maxAttempts = 3;

            try
            {
                var queueResponse = await Client.CreateQueueAsync(QueueName).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(queueResponse?.QueueUrl))
                {
                    Uri = new Uri(queueResponse.QueueUrl);
                    await Client.SetQueueAttributesAsync(queueResponse.QueueUrl, GetCreateQueueAttributes(queueConfig)).ConfigureAwait(false);
                    await SetQueuePropertiesAsync().ConfigureAwait(false);

                    Logger.LogInformation("Created queue '{QueueName}' with ARN '{Arn}'.", QueueName, Arn);
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    if (attempt >= (maxAttempts - 1))
                    {
                        Logger.LogError(
                            ex,
                            "Error trying to create queue '{QueueName}'. Maximum retries of {MaxAttempts} exceeded for delay {Delay}.",
                            QueueName, maxAttempts,
                            CreateRetryDelay);

                        throw;
                    }

                    // Ensure we wait for queue delete timeout to expire.
                    Logger.LogInformation(
                        "Waiting to create queue '{QueueName}' for {Delay}, due to AWS time restriction. Attempt number {AttemptCount} of {MaxAttempts}.",
                        QueueName,
                        CreateRetryDelay,
                        attempt + 1,
                        maxAttempts);

                    await Task.Delay(CreateRetryDelay).ConfigureAwait(false);
                    await CreateAsync(queueConfig, attempt + 1).ConfigureAwait(false);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    Logger.LogError(ex, "Error trying to create queue '{QueueName}'.", QueueName);
                    throw;
                }
            }

            Logger.LogWarning("Failed to create queue '{QueueName}'.", QueueName);
            return false;
        }

        protected abstract Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig);

        public virtual async Task DeleteAsync()
        {
            var exists = await ExistsAsync().ConfigureAwait(false);
            if (exists)
            {
                var request = new DeleteQueueRequest
                {
                    QueueUrl = Uri.AbsoluteUri
                };
                await Client.DeleteQueueAsync(request).ConfigureAwait(false);
            }
        }

        private async Task SetQueuePropertiesAsync()
        {
            var keys = new[]
            {
                JustSayingConstants.AttributeArn,
                JustSayingConstants.AttributeRedrivePolicy,
                JustSayingConstants.AttributePolicy,
                JustSayingConstants.AttributeRetentionPeriod,
                JustSayingConstants.AttributeVisibilityTimeout,
                JustSayingConstants.AttributeDeliveryDelay,
                JustSayingConstants.AttributeEncryptionKeyId,
                JustSayingConstants.AttributeEncryptionKeyReusePeriodSecondId
            };
            var attributes = await GetAttrsAsync(keys).ConfigureAwait(false);
            Arn = attributes.QueueARN;
            MessageRetentionPeriod = TimeSpan.FromSeconds(attributes.MessageRetentionPeriod);
            _visibilityTimeout = TimeSpan.FromSeconds(attributes.VisibilityTimeout);
            Policy = attributes.Policy;
            DeliveryDelay = TimeSpan.FromSeconds(attributes.DelaySeconds);
            RedrivePolicy = ExtractRedrivePolicyFromQueueAttributes(attributes.Attributes);
            ServerSideEncryption = ExtractServerSideEncryptionFromQueueAttributes(attributes.Attributes);
        }

        private async Task<GetQueueAttributesResponse> GetAttrsAsync(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest
            {
                QueueUrl = Uri.AbsoluteUri,
                AttributeNames = new List<string>(attrKeys)
            };

            return await Client.GetQueueAttributesAsync(request).ConfigureAwait(false);
        }

        public virtual async Task UpdateQueueAttributeAsync(SqsBasicConfiguration queueConfig)
        {
            if (QueueNeedsUpdating(queueConfig))
            {
                var attributes = new Dictionary<string, string>
                {
                    {JustSayingConstants.AttributeRetentionPeriod, queueConfig.MessageRetention.AsSecondsString() },
                    {JustSayingConstants.AttributeVisibilityTimeout, queueConfig.VisibilityTimeout.AsSecondsString() },
                    {JustSayingConstants.AttributeDeliveryDelay, queueConfig.DeliveryDelay.AsSecondsString() }
                };

                if (queueConfig.ServerSideEncryption != null)
                {
                    attributes.Add(JustSayingConstants.AttributeEncryptionKeyId, queueConfig.ServerSideEncryption.KmsMasterKeyId);
                    attributes.Add(JustSayingConstants.AttributeEncryptionKeyReusePeriodSecondId, queueConfig.ServerSideEncryption.KmsDataKeyReusePeriodSeconds);
                }

                if (queueConfig.ServerSideEncryption == null)
                {
                    attributes.Add(JustSayingConstants.AttributeEncryptionKeyId, string.Empty);
                }

                var request = new SetQueueAttributesRequest
                {
                    QueueUrl = Uri.AbsoluteUri,
                    Attributes = attributes
                };

                var response = await Client.SetQueueAttributesAsync(request).ConfigureAwait(false);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    MessageRetentionPeriod = queueConfig.MessageRetention;
                    _visibilityTimeout = queueConfig.VisibilityTimeout;
                    DeliveryDelay = queueConfig.DeliveryDelay;
                    ServerSideEncryption = queueConfig.ServerSideEncryption;
                }
            }
        }

        protected virtual bool QueueNeedsUpdating(SqsBasicConfiguration queueConfig)
        {
            return MessageRetentionPeriod != queueConfig.MessageRetention
                   || _visibilityTimeout != queueConfig.VisibilityTimeout
                   || DeliveryDelay != queueConfig.DeliveryDelay
                   || QueueNeedsUpdatingBecauseOfEncryption(queueConfig);
        }

        private bool QueueNeedsUpdatingBecauseOfEncryption(SqsBasicConfiguration queueConfig)
        {
            if (ServerSideEncryption == queueConfig.ServerSideEncryption)
            {
                return false;
            }

            if (ServerSideEncryption != null && queueConfig.ServerSideEncryption != null)
            {
                return ServerSideEncryption.KmsMasterKeyId != queueConfig.ServerSideEncryption.KmsMasterKeyId ||
                       ServerSideEncryption.KmsDataKeyReusePeriodSeconds != queueConfig.ServerSideEncryption.KmsDataKeyReusePeriodSeconds;
            }

            return true;
        }

        private static RedrivePolicy ExtractRedrivePolicyFromQueueAttributes(Dictionary<string, string> queueAttributes)
        {
            if (!queueAttributes.ContainsKey(JustSayingConstants.AttributeRedrivePolicy))
            {
                return null;
            }

            return RedrivePolicy.ConvertFromString(queueAttributes[JustSayingConstants.AttributeRedrivePolicy]);
        }

        private static ServerSideEncryption ExtractServerSideEncryptionFromQueueAttributes(Dictionary<string, string> queueAttributes)
        {
            if (!queueAttributes.ContainsKey(JustSayingConstants.AttributeEncryptionKeyId))
            {
                return null;
            }

            return new ServerSideEncryption
            {
                KmsMasterKeyId = queueAttributes[JustSayingConstants.AttributeEncryptionKeyId],
                KmsDataKeyReusePeriodSeconds = queueAttributes[JustSayingConstants.AttributeEncryptionKeyReusePeriodSecondId]
            };
        }

        public virtual InterrogationResult Interrogate()
        {
            return new InterrogationResult(new
            {
                Arn,
                QueueName,
                Region = _region.SystemName,
                Policy,
                Uri,
                DeliveryDelay,
                VisibilityTimeout = _visibilityTimeout,
                MessageRetentionPeriod,
            });
        }
    }
}
