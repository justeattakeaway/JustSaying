using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.SQS;
using Amazon.SQS.Util;

namespace JustSaying.AwsTools
{
    public class ErrorQueue : SqsQueueByNameBase
    {
        public ErrorQueue(string sourceQueueName, IAmazonSQS client)
            : base(sourceQueueName + "_error", client)
        {
            ErrorQueue = null;
        }

        protected override Dictionary<string, string> GetCreateQueueAttributes(int retentionPeriodSeconds, int visibilityTimeoutSeconds)
        {
            return new Dictionary<string, string>
            {
                {SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD, retentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)},
                {SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT, visibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture)}
            };
        }

        public override bool Create(
            int retentionPeriodSeconds,
            int attempt = JustSayingConstants.DEFAULT_CREATE_REATTEMPT,
            int visibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT,
            bool errorQueueOptOut = false,
            int retryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT)
        {
            if (!errorQueueOptOut)
                throw new InvalidOperationException("Cannot create a dead letter queue for a dead letter queue.");

            return base.Create(retentionPeriodSeconds, attempt, visibilityTimeoutSeconds, true);
        }
    }
}