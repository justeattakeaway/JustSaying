using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using NLog;

namespace JustSaying.AwsTools
{
    public abstract class SqsQueueByNameBase : SqsQueueBase
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

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
            Url = result.QueueUrls.SingleOrDefault(x => Matches(x, QueueNamePrefix));

            if (Url != null)
            {
                SetArn();
                return true;
            }

            return false;
        }
        private static bool Matches(string queueUrl, string queueName)
        {
            return queueUrl.Substring(queueUrl.LastIndexOf("/", StringComparison.InvariantCulture) + 1)
                .Equals(queueName, StringComparison.InvariantCultureIgnoreCase);
        }

        public virtual bool Create(SqsConfiguration queueConfig, int attempt = 0)
        {
            try
            {
                var result = Client.CreateQueue(new CreateQueueRequest{
                    QueueName = QueueNamePrefix,
                    Attributes = GetCreateQueueAttributes(queueConfig.MessageRetentionSeconds, queueConfig.VisibilityTimeoutSeconds)});

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
                    Create(queueConfig, attempt: attempt++);
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