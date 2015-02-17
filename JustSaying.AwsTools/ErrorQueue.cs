using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.QueueCreation;
using NLog;

namespace JustSaying.AwsTools
{
    public class ErrorQueue : SqsQueueByNameBase
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");
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

            try
            {
                var result = Client.CreateQueue(new CreateQueueRequest
                {
                    QueueName = QueueName,
                    Attributes = GetCreateQueueAttributes(queueConfig.ErrorQueueRetentionPeriodSeconds, JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT)
                });

                if (!string.IsNullOrWhiteSpace(result.QueueUrl))
                {
                    Url = result.QueueUrl;
                    SetQueueProperties();

                    Log.Info(string.Format("Created Queue: {0} on Arn: {1}", QueueName, Arn));
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    Log.Info(string.Format("Waiting to create Queue due to AWS time restriction - Queue: {0}, AttemptCount: {1}", QueueName, attempt + 1));
                    Thread.Sleep(60000);
                    Create(queueConfig, attempt: attempt++);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    Log.ErrorException(string.Format("Create Queue error: {0}", QueueName), ex);
                    throw;
                }

                // If we're on a delete timeout, throw after 2 attempts.
                if (attempt >= 2)
                {
                    Log.ErrorException(string.Format("Create Queue error, max retries exceeded for delay - Queue: {0}", QueueName), ex);
                    throw;
                }
            }

            Log.Info(string.Format("Failed to create Queue: {0}", QueueName));
            return false;

        }

        protected internal override void UpdateQueueAttribute(SqsBasicConfiguration queueConfig)
        {
            if (QueueNeedsUpdating(queueConfig))
            {
                var response = Client.SetQueueAttributes(
                    new SetQueueAttributesRequest
                    {
                        QueueUrl = Url,
                        Attributes = new Dictionary<string, string>
                        {
                            {JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD, queueConfig.ErrorQueueRetentionPeriodSeconds.ToString()},
                        }
                    });
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