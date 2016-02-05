using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.AwsTools
{
    public class ErrorQueue : SqsQueueByNameBase
    {
        public ErrorQueue(RegionEndpoint region, string sourceQueueName, IAmazonSQS client)
            : base(region, sourceQueueName + "_error", client)
        {
            ErrorQueue = null;
        }

        protected override Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig)
        {
            return new Dictionary<string, string>
            {
                { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD , queueConfig.ErrorQueueRetentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT.ToString(CultureInfo.InvariantCulture)},
            };
        }

        protected internal override void UpdateQueueAttribute(SqsBasicConfiguration queueConfig)
        {
            if (QueueNeedsUpdating(queueConfig))
            {
                var request = new SetQueueAttributesRequest
                {
                    QueueUrl = Url,
                    Attributes = new Dictionary<string, string>
                    {
                        {
                            JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD,
                            queueConfig.ErrorQueueRetentionPeriodSeconds.ToString()
                        }
                    }
                };
                var response = Client.SetQueueAttributes(request);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    MessageRetentionPeriod = queueConfig.ErrorQueueRetentionPeriodSeconds;
                }
            }
        }

        protected override bool QueueNeedsUpdating(SqsBasicConfiguration queueConfig)
        {
            return MessageRetentionPeriod != queueConfig.ErrorQueueRetentionPeriodSeconds;
        }
    }
}