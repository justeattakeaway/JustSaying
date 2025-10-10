using System.Collections.Generic;
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
        public string Url { get; protected set; }
        public IAmazonSQS Client { get; private set; }
        public string QueueName { get; protected set; }
        public RegionEndpoint Region { get; protected set; }
        public ErrorQueue ErrorQueue { get; protected set; }
        internal int MessageRetentionPeriod { get; set; }
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
            Url = null;

            var exists = await ExistsAsync().ConfigureAwait(false);
            if (exists)
            {
                await Client.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = Url }).ConfigureAwait(false);

                Arn = null;
                Url = null;
            }
        }

        protected async Task SetQueuePropertiesAsync()
        {
            var keys = new[]
                {
                    JustSayingConstants.ATTRIBUTE_ARN,
                    JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY,
                    JustSayingConstants.ATTRIBUTE_POLICY,
                    JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD,
                    JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT,
                    JustSayingConstants.ATTRIBUTE_DELIVERY_DELAY,
                    JustSayingConstants.ATTRIBUTE_ENCRYPTION_KEY_ID,
                    JustSayingConstants.ATTRIBUTE_ENCRYPTION_KEY_REUSE_PERIOD_SECOND_ID
                };
            var attributes = await GetAttrsAsync(keys).ConfigureAwait(false);
            Arn = attributes.QueueARN;
            MessageRetentionPeriod = attributes.MessageRetentionPeriod;
            VisibilityTimeout = attributes.VisibilityTimeout;
            Policy = attributes.Policy;
            DeliveryDelay = attributes.DelaySeconds;
            RedrivePolicy = ExtractRedrivePolicyFromQueueAttributes(attributes.Attributes);
            ServerSideEncryption = ExtractServerSideEncryptionFromQueueAttributes(attributes.Attributes);
        }

        protected async Task<GetQueueAttributesResponse> GetAttrsAsync(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest {
                QueueUrl = Url,
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
                    {JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD, queueConfig.MessageRetentionSeconds.ToString()},
                    {JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT, queueConfig.VisibilityTimeoutSeconds.ToString()},
                    {JustSayingConstants.ATTRIBUTE_DELIVERY_DELAY, queueConfig.DeliveryDelaySeconds.ToString()}
                };
                if (queueConfig.ServerSideEncryption != null)
                {
                    attributes.Add(JustSayingConstants.ATTRIBUTE_ENCRYPTION_KEY_ID, queueConfig.ServerSideEncryption.KmsMasterKeyId);
                    attributes.Add(JustSayingConstants.ATTRIBUTE_ENCRYPTION_KEY_REUSE_PERIOD_SECOND_ID, queueConfig.ServerSideEncryption.KmsDataKeyReusePeriodSeconds);
                }

                if (queueConfig.ServerSideEncryption == null)
                {
                    attributes.Add(JustSayingConstants.ATTRIBUTE_ENCRYPTION_KEY_ID, string.Empty);
                }
                var request = new SetQueueAttributesRequest
                {
                    QueueUrl = Url,
                    Attributes = attributes
                };

                var response = await Client.SetQueueAttributesAsync(request).ConfigureAwait(false);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    MessageRetentionPeriod = queueConfig.MessageRetentionSeconds;
                    VisibilityTimeout = queueConfig.VisibilityTimeoutSeconds;
                    DeliveryDelay = queueConfig.DeliveryDelaySeconds;
                    ServerSideEncryption = queueConfig.ServerSideEncryption;
                }
            }
        }

        protected virtual bool QueueNeedsUpdating(SqsBasicConfiguration queueConfig)
        {
            return MessageRetentionPeriod != queueConfig.MessageRetentionSeconds
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

        private RedrivePolicy ExtractRedrivePolicyFromQueueAttributes(Dictionary<string, string> queueAttributes)
        {
            if (queueAttributes != null && queueAttributes.TryGetValue(JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, out var redrivePolicy))
            {
                return RedrivePolicy.ConvertFromString(redrivePolicy);
            }

            return null;
        }

        private ServerSideEncryption ExtractServerSideEncryptionFromQueueAttributes(Dictionary<string, string> queueAttributes)
        {
            if (queueAttributes is not null && queueAttributes.TryGetValue(JustSayingConstants.ATTRIBUTE_ENCRYPTION_KEY_ID, out var encryptionKeyId))
            {
                queueAttributes.TryGetValue(JustSayingConstants.ATTRIBUTE_ENCRYPTION_KEY_REUSE_PERIOD_SECOND_ID, out var reusePeriod);
                return new ServerSideEncryption
                {
                    KmsMasterKeyId = encryptionKeyId,
                    KmsDataKeyReusePeriodSeconds = reusePeriod
                };
            }

            return null;
        }
    }
}
