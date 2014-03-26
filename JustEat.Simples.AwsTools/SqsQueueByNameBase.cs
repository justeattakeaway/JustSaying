using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using NLog;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public abstract class SqsQueueByNameBase : SqsQueueBase
    {
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public SqsQueueByNameBase(string queueName, IAmazonSQS client)
            : base(client)
        {
            QueueNamePrefix = queueName;
            Exists();
        }

        public override bool Exists()
        {
            var result = Client.ListQueues(new ListQueuesRequest{ QueueNamePrefix = QueueNamePrefix });
            Console.WriteLine("polling for {0}", QueueNamePrefix);
            if (result.QueueUrls.Any())
            {
                Console.WriteLine("found " + result.QueueUrls.First());
                Url = result.QueueUrls.First();
                SetArn();
                return true;
            }

            return false;
        }

        public virtual bool Create(int retentionPeriodSeconds, int attempt = 0, int visibilityTimeoutSeconds = 30, bool errorQueueOptOut = false, int retryCountBeforeSendingToErrorQueue = NotificationStackConstants.DEFAULT_HANDLER_RETRY_COUNT)
        {
            try
            {
                var result = Client.CreateQueue(new CreateQueueRequest{
                    QueueName = QueueNamePrefix,
                    Attributes = GetCreateQueueAttributes(retentionPeriodSeconds, visibilityTimeoutSeconds)});

                if (!string.IsNullOrWhiteSpace(result.QueueUrl))
                {
                    Url = result.QueueUrl;
                    SetArn();

                    Log.Info(string.Format("Created Queue: {0} on Arn: {1}", QueueNamePrefix, Arn));
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    Log.Info(string.Format("Waiting to create Queue due to AWS time restriction - Queue: {0}, AttemptCount: {1}", QueueNamePrefix, attempt + 1));
                    Thread.Sleep(60000);
                    Create(retentionPeriodSeconds, attempt++, visibilityTimeoutSeconds);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    Log.ErrorException(string.Format("Create Queue error: {0}", QueueNamePrefix), ex);
                    throw;
                }

                // If we're on a delete timeout, throw after 2 attempts.
                if (attempt >= 2)
                {
                    Log.ErrorException(string.Format("Create Queue error, max retries exceeded for delay - Queue: {0}", QueueNamePrefix), ex);
                    throw;
                }
            }

            Log.Info(string.Format("Failed to create Queue: {0}", QueueNamePrefix));
            return false;
        }

        protected abstract Dictionary<string, string> GetCreateQueueAttributes(int retentionPeriodSeconds,
            int visibilityTimeoutSeconds);
    }
}