using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SqsQueueByNameBase : SqsQueueBase
    {
        private readonly ILogger _log;

        protected SqsQueueByNameBase(RegionEndpoint region, string queueName, IAmazonSQS client, ILoggerFactory loggerFactory)
            : base(region, client)
        {
            QueueName = queueName;
            _log = loggerFactory.CreateLogger("JustSaying");
        }

        public override async Task<bool> ExistsAsync()
        {
            var result = await Client.ListQueuesAsync(new ListQueuesRequest{ QueueNamePrefix = QueueName });

            _log.LogInformation($"Checking if queue '{QueueName}' exists");
            Url = result?.QueueUrls?.SingleOrDefault(x => Matches(x, QueueName));

            if (Url == null) return false;
            await SetQueuePropertiesAsync();
            return true;
        }

        private static bool Matches(string queueUrl, string queueName)
            => queueUrl.Substring(queueUrl.LastIndexOf("/", StringComparison.Ordinal) + 1)
                .Equals(queueName, StringComparison.OrdinalIgnoreCase);
        
        public virtual async Task<bool> CreateAsync(SqsBasicConfiguration queueConfig, int attempt = 0)
        {
            try
            {
                var queueResponse = await Client.CreateQueueAsync(QueueName);

                if (!string.IsNullOrWhiteSpace(queueResponse?.QueueUrl))
                {
                    Url = queueResponse.QueueUrl;
                    await Client.SetQueueAttributesAsync(queueResponse.QueueUrl, GetCreateQueueAttributes(queueConfig));
                    await SetQueuePropertiesAsync();

                    _log.LogInformation($"Created Queue: {QueueName} on Arn: {Arn}");
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    _log.LogInformation($"Waiting to create Queue due to AWS time restriction - Queue: {QueueName}, AttemptCount: {attempt + 1}");
                    await Task.Delay(60000);
                    await CreateAsync(queueConfig, attempt + 1);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    _log.LogError(0, (Exception) ex, $"Create Queue error: {QueueName}");
                    throw;
                }

                // If we're on a delete timeout, throw after 2 attempts.
                if (attempt >= 2)
                {
                    _log.LogError(0, (Exception) ex, $"Create Queue error, max retries exceeded for delay - Queue: {QueueName}");
                    throw;
                }
            }

            _log.LogInformation($"Failed to create Queue: {QueueName}");
            return false;
        }

        protected abstract Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig);
    }
}
