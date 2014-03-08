using System.Collections.Generic;
using System.Globalization;
using Amazon.SQS;
using Amazon.SQS.Util;

namespace JustEat.Simples.NotificationStack.AwsTools
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
                { NotificationStackConstants.ATTRIBUTE_REDRIVE_POLICY, "{\"maxReceiveCount\":\"1\", \"deadLetterTargetArn\":\""+ErrorQueue.Arn+"\"}"}
            };
        }

        public override bool Create(int retentionPeriodSeconds, int attempt = NotificationStackConstants.DEFAULT_CREATE_REATTEMPT, int visibilityTimeoutSeconds = NotificationStackConstants.DEFAULT_VISIBILITY_TIMEOUT, bool createErrorQueue = false, int retryCountBeforeSendingToErrorQueue = NotificationStackConstants.DEFAULT_HANDLER_RETRY_COUNT)
        {
            if (!ErrorQueue.Exists())
            {
                ErrorQueue.Create(NotificationStackConstants.MAXIMUM_RETENTION_PERIOD, NotificationStackConstants.DEFAULT_CREATE_REATTEMPT, NotificationStackConstants.DEFAULT_VISIBILITY_TIMEOUT, errorQueueOptOut: true);
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