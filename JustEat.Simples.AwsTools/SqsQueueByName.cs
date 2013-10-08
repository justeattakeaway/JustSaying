using System.Globalization;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using NLog;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public class SqsQueueByName : SqsQueueBase
    {
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public SqsQueueByName(string queueName, AmazonSQS client)
            : base(client)
        {
            QueueNamePrefix = queueName;
            Exists();
        }

        public override bool Exists()
        {
            var result = Client.ListQueues(new ListQueuesRequest().WithQueueNamePrefix(QueueNamePrefix));
            if (result.IsSetListQueuesResult() && result.ListQueuesResult.IsSetQueueUrl())
            {
                Url = result.ListQueuesResult.QueueUrl.First();
                SetArn();
                return true;
            }

            return false;
        }

        public bool Create(int retentionPeriodSeconds, int attempt = 0, int visibilityTimeoutSeconds = 30)
        {
            try
            {
                var result = Client.CreateQueue(new CreateQueueRequest()
                    .WithQueueName(QueueNamePrefix)
                    .WithAttribute(new[]
                    {
                        new Attribute { Name = SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD , Value = retentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)},
                        new Attribute { Name = SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , Value = visibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture)},
                    }));

                if (result.IsSetCreateQueueResult() && !string.IsNullOrWhiteSpace(result.CreateQueueResult.QueueUrl))
                {
                    Url = result.CreateQueueResult.QueueUrl;
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
                    Create(attempt++);
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
    }
}