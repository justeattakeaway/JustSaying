using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.SQS;
using Amazon.SQS.Util;
using JustSaying.AwsTools.QueueCreation;

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

        public override bool Create(SqsBasicConfiguration queueConfig, int attempt = 0)
        {
            if (!queueConfig.ErrorQueueOptOut)
                throw new InvalidOperationException("Cannot create a dead letter queue for a dead letter queue.");

            return base.Create(queueConfig, attempt: attempt);
        }
    }
}