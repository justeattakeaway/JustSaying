using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools
{
    public class ErrorQueue : SqsQueueByNameBase
    {
        public ErrorQueue(RegionEndpoint region, string sourceQueueName, IAmazonSQS client, ILoggerFactory loggerFactory)
            : base(region, sourceQueueName + "_error", client, loggerFactory)
        {
            ErrorQueue = null;
        }

        protected override Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig)
        {
            return new Dictionary<string, string>
            {
                { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD , queueConfig.ErrorQueueRetentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , JustSayingConstants.DefaultVisibilityTimeout.ToString(CultureInfo.InvariantCulture)},
            };
        }

        public override async Task UpdateQueueAttributeAsync(SqsBasicConfiguration queueConfig)
        {
            if (!QueueNeedsUpdating(queueConfig))
            {
                return;
            }

            var request = new SetQueueAttributesRequest
            {
                QueueUrl = Url.ToString(),
                Attributes = new Dictionary<string, string>
                {
                    {
                        JustSayingConstants.AttributeRetentionPeriod,
                        queueConfig.ErrorQueueRetentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)
                    }
                }
            };

            var response = await Client.SetQueueAttributesAsync(request).ConfigureAwait(false);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                MessageRetentionPeriod = queueConfig.ErrorQueueRetentionPeriodSeconds;
            }
        }

        protected override bool QueueNeedsUpdating(SqsBasicConfiguration queueConfig)
            => MessageRetentionPeriod != queueConfig.ErrorQueueRetentionPeriodSeconds;
    }
}
