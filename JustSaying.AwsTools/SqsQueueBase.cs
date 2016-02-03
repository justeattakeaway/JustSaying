using System.Collections.Generic;
using System.Net;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using NLog;

namespace JustSaying.AwsTools
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
        
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        protected SqsQueueBase(RegionEndpoint region, IAmazonSQS client)
        {
            Region = region;
            Client = client;
        }

        public abstract bool Exists();

        public virtual void Delete()
        {
            Arn = null;
            Url = null;

            if (!Exists())
            {
                return;
            }

            Client.DeleteQueue(new DeleteQueueRequest { QueueUrl = Url });
            
            Arn = null;
            Url = null;
        }

        protected void SetQueueProperties()
        {
            var attributes = GetAttrs(new[]
            {
                JustSayingConstants.ATTRIBUTE_ARN, 
                JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY,
                JustSayingConstants.ATTRIBUTE_POLICY,
                JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD,
                JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT,
                JustSayingConstants.ATTRIBUTE_DELIVERY_DELAY
            });
            Arn = attributes.QueueARN;
            MessageRetentionPeriod = attributes.MessageRetentionPeriod;
            VisibilityTimeout = attributes.VisibilityTimeout;
            DeliveryDelay = attributes.DelaySeconds;
            RedrivePolicy = ExtractRedrivePolicyFromQueueAttributes(attributes.Attributes);
        }

        protected GetQueueAttributesResult GetAttrs(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest { 
                QueueUrl = Url,
                AttributeNames = new List<string>(attrKeys)
            };
            
            var result = Client.GetQueueAttributes(request);

            return result;
        }

        protected internal virtual void UpdateQueueAttribute(SqsBasicConfiguration queueConfig)
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
                var response = Client.SetQueueAttributes(request);
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
