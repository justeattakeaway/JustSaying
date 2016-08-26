using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using NLog;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SqsQueueByNameBase : SqsQueueBase
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        protected SqsQueueByNameBase(RegionEndpoint region, string queueName, IAmazonSQS client)
            : base(region, client)
        {
            QueueName = queueName;
        }

        public override bool Exists()
        {
            // todo make this async
            var result = Client.ListQueuesAsync(new ListQueuesRequest{ QueueNamePrefix = QueueName })
                .GetAwaiter().GetResult();

            Log.Info($"Checking if queue '{QueueName}' exists");
            Url = result?.QueueUrls?.SingleOrDefault(x => Matches(x, QueueName));

            if (Url != null)
            {
                SetQueueProperties();
                return true;
            }

            return false;
        }

        private static bool Matches(string queueUrl, string queueName)
            => queueUrl.Substring(queueUrl.LastIndexOf("/", StringComparison.Ordinal) + 1)
                .Equals(queueName, StringComparison.OrdinalIgnoreCase);

        public virtual bool Create(SqsBasicConfiguration queueConfig, int attempt = 0)
        {
            try
            {
                // todo make this async
                var result = Client.CreateQueueAsync(new CreateQueueRequest{
                    QueueName = QueueName,
                    Attributes = GetCreateQueueAttributes(queueConfig)})
                    .GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(result?.QueueUrl))
                {
                    Url = result.QueueUrl;
                    SetQueueProperties();

                    Log.Info($"Created Queue: {QueueName} on Arn: {Arn}");
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    Log.Info(
                        $"Waiting to create Queue due to AWS time restriction - Queue: {QueueName}, AttemptCount: {attempt + 1}");
                    Thread.Sleep(60000);
                    Create(queueConfig, attempt: attempt++);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    Log.Error(ex, $"Create Queue error: {QueueName}");
                    throw;
                }

                // If we're on a delete timeout, throw after 2 attempts.
                if (attempt >= 2)
                {
                    Log.Error(ex, $"Create Queue error, max retries exceeded for delay - Queue: {QueueName}");
                    throw;
                }
            }

            Log.Info($"Failed to create Queue: {QueueName}");
            return false;
        }

        protected abstract Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig);
    }
}
