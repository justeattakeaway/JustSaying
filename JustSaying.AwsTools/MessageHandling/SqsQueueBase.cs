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
        public string Policy { get; private set; }


        protected SqsQueueBase(RegionEndpoint region, IAmazonSQS client)
        {
            Region = region;
            Client = client;
        }

        public bool Exists()
        {
            return ExistsAsync()
                .GetAwaiter().GetResult();
        }

        public abstract Task<bool> ExistsAsync();

        public void Delete()
        {
            DeleteAsync()
                .GetAwaiter().GetResult();
        }


        public virtual async Task DeleteAsync()
        {
            Arn = null;
            Url = null;

            var exists = await ExistsAsync();
            if (exists)
            {
                await Client.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = Url });

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
                    JustSayingConstants.ATTRIBUTE_DELIVERY_DELAY
                };
            var attributes = await GetAttrsAsync(keys);
            Arn = attributes.QueueARN;
            MessageRetentionPeriod = attributes.MessageRetentionPeriod;
            VisibilityTimeout = attributes.VisibilityTimeout;
            Policy = attributes.Policy;
            DeliveryDelay = attributes.DelaySeconds;
            RedrivePolicy = ExtractRedrivePolicyFromQueueAttributes(attributes.Attributes);
        }

        protected GetQueueAttributesResponse GetAttrs(IEnumerable<string> attrKeys)
        {
            return GetAttrsAsync(attrKeys)
                .GetAwaiter().GetResult();
        }

        protected async Task<GetQueueAttributesResponse> GetAttrsAsync(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest {
                QueueUrl = Url,
                AttributeNames = new List<string>(attrKeys)
            };

            return await Client.GetQueueAttributesAsync(request);
        }

        public void UpdateQueueAttribute(SqsBasicConfiguration queueConfig)
        {
            UpdateQueueAttributeAsync(queueConfig)
                .GetAwaiter().GetResult();
        }

        protected internal virtual async Task UpdateQueueAttributeAsync(SqsBasicConfiguration queueConfig)
        {
            if (QueueNeedsUpdating(queueConfig))
            {
                var request = new SetQueueAttributesRequest
                {
                    QueueUrl = Url,
                    Attributes = new Dictionary<string, string>
                    {
                        {JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD, queueConfig.MessageRetentionSeconds.ToString()},
                        {
                            JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT,
                            queueConfig.VisibilityTimeoutSeconds.ToString()
                        },
                        {JustSayingConstants.ATTRIBUTE_DELIVERY_DELAY, queueConfig.DeliveryDelaySeconds.ToString()}
                    }
                };

                var response = await Client.SetQueueAttributesAsync(request);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    MessageRetentionPeriod = queueConfig.MessageRetentionSeconds;
                    VisibilityTimeout = queueConfig.VisibilityTimeoutSeconds;
                    DeliveryDelay = queueConfig.DeliveryDelaySeconds;
                }
            }
        }

        protected virtual bool QueueNeedsUpdating(SqsBasicConfiguration queueConfig)
        {
            return MessageRetentionPeriod != queueConfig.MessageRetentionSeconds
                   || VisibilityTimeout != queueConfig.VisibilityTimeoutSeconds
                   || DeliveryDelay != queueConfig.DeliveryDelaySeconds;
        }

        private RedrivePolicy ExtractRedrivePolicyFromQueueAttributes(Dictionary<string, string> queueAttributes)
        {
            if (!queueAttributes.ContainsKey(JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY))
            {
                return null;
            }
            return RedrivePolicy.ConvertFromString(queueAttributes[JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY]);
        }
    }
}
