using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SqsQueueBase
    {
        public string Arn { get; protected set; }
        public Uri Uri { get; protected set; }
        public IAmazonSQS Client { get; private set; }
        public string QueueName { get; protected set; }
        public RegionEndpoint Region { get; protected set; }
        public ErrorQueue ErrorQueue { get; protected set; }
        internal TimeSpan MessageRetentionPeriod { get; set; }
        internal int VisibilityTimeout { get; set; }
        internal int DeliveryDelay { get; set; }
        internal RedrivePolicy RedrivePolicy { get; set; }
        internal ServerSideEncryption ServerSideEncryption { get; set; }
        public string Policy { get; private set; }


        protected SqsQueueBase(RegionEndpoint region, IAmazonSQS client)
        {
            Region = region;
            Client = client;
        }

        public abstract Task<bool> ExistsAsync();

        public virtual async Task DeleteAsync()
        {
            Arn = null;
            Uri = null;

            var exists = await ExistsAsync().ConfigureAwait(false);
            if (exists)
            {
                var request = new DeleteQueueRequest
                    {
                        QueueUrl = Uri.AbsoluteUri
                };
                await Client.DeleteQueueAsync(request).ConfigureAwait(false);

                Arn = null;
                Uri = null;
            }
        }

        protected async Task SetQueuePropertiesAsync()
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
            VisibilityTimeout = attributes.VisibilityTimeout;
            Policy = attributes.Policy;
            DeliveryDelay = attributes.DelaySeconds;
            RedrivePolicy = ExtractRedrivePolicyFromQueueAttributes(attributes.Attributes);
            ServerSideEncryption = ExtractServerSideEncryptionFromQueueAttributes(attributes.Attributes);
        }

        protected async Task<GetQueueAttributesResponse> GetAttrsAsync(IEnumerable<string> attrKeys)
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
                    {JustSayingConstants.AttributeRetentionPeriod, queueConfig.MessageRetention.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture) },
                    {JustSayingConstants.AttributeVisibilityTimeout, queueConfig.VisibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture) },
                    {JustSayingConstants.AttributeDeliveryDelay, queueConfig.DeliveryDelaySeconds.ToString(CultureInfo.InvariantCulture) }
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
                    VisibilityTimeout = queueConfig.VisibilityTimeoutSeconds;
                    DeliveryDelay = queueConfig.DeliveryDelaySeconds;
                    ServerSideEncryption = queueConfig.ServerSideEncryption;
                }
            }
        }

        protected virtual bool QueueNeedsUpdating(SqsBasicConfiguration queueConfig)
        {
            return MessageRetentionPeriod != queueConfig.MessageRetention
                   || VisibilityTimeout != queueConfig.VisibilityTimeoutSeconds
                   || DeliveryDelay != queueConfig.DeliveryDelaySeconds
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

        private ServerSideEncryption ExtractServerSideEncryptionFromQueueAttributes(Dictionary<string, string> queueAttributes)
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
    }
}
