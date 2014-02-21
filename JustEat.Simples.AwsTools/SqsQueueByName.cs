using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using NLog;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public static class NotificationStackConstants
    {
        public const string ATTRIBUTE_REDRIVE_POLICY = "RedrivePolicy";
        public const int DEFAULT_CREATE_REATTEMPT = 0;
        public const int DEFAULT_VISIBILITY_TIMEOUT = 30;
        public const int MAXIMUM_RETENTION_PERIOD = 1209600;    //14 days

    }
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

        public override bool Create(
            int retentionPeriodSeconds, 
            int attempt = NotificationStackConstants.DEFAULT_CREATE_REATTEMPT, 
            int visibilityTimeoutSeconds = NotificationStackConstants.DEFAULT_VISIBILITY_TIMEOUT,
            bool createErrorQueue = true)
        {
            if (!ErrorQueue.Exists())
            {
                ErrorQueue.Create(NotificationStackConstants.MAXIMUM_RETENTION_PERIOD, NotificationStackConstants.DEFAULT_CREATE_REATTEMPT, NotificationStackConstants.DEFAULT_VISIBILITY_TIMEOUT, createErrorQueue: false);
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
            if (result.QueueUrls.Any())
            {
                Url = result.QueueUrls.First();
                SetArn();
                return true;
            }

            return false;
        }

        public virtual bool Create(int retentionPeriodSeconds, int attempt = 0, int visibilityTimeoutSeconds = 30, bool createErrorQueue = true)
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