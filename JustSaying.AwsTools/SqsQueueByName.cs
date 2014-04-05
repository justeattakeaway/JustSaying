using System.Collections.Generic;
using System.Globalization;
using Amazon.SQS;
using Amazon.SQS.Util;

namespace JustSaying.AwsTools
{
    public class SqsQueueByName : SqsQueueByNameBase
    {
        public SqsQueueByName(string queueName, IAmazonSQS client)
            : base(queueName, client)
        {
            ErrorQueue = new ErrorQueue(queueName, client);
        }

        protected override Dictionary<string, string> GetCreateQueueAttributes(int retentionPeriodSeconds, int visibilityTimeoutSeconds)
        {
            return new Dictionary<string, string>
            {
                { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD , retentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , visibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture)},
                { JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, "{\"maxReceiveCount\":\"1\", \"deadLetterTargetArn\":\""+ErrorQueue.Arn+"\"}"}
            };
        }

        public override bool Create(int retentionPeriodSeconds, int attempt = JustSayingConstants.DEFAULT_CREATE_REATTEMPT, int visibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT, bool createErrorQueue = false, int retryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT)
        {
            if (!ErrorQueue.Exists())
            {
                ErrorQueue.Create(JustSayingConstants.MAXIMUM_RETENTION_PERIOD, JustSayingConstants.DEFAULT_CREATE_REATTEMPT, JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT, errorQueueOptOut: true);
            }
            return base.Create(retentionPeriodSeconds, attempt, visibilityTimeoutSeconds);
        }

        public override void Delete()
        {
            if(ErrorQueue != null)
                ErrorQueue.Delete();
            base.Delete();
        }
    }
}