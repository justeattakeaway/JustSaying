using System;
using System.Collections.Generic;
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
            GetQueueUrlResponse result;
            _log.LogInformation("Checking if {QueueName} exists", QueueName);
            if (string.IsNullOrWhiteSpace(QueueName))
            {
                return false;
            }

            try
            {
                result = await Client.GetQueueUrlAsync(QueueName).ConfigureAwait(false);
            }
            catch (QueueDoesNotExistException)
            {
                return false;
            }

            if (result?.QueueUrl == null)
            {
                return false;
            }

            Uri = new Uri(result.QueueUrl);

            await SetQueuePropertiesAsync().ConfigureAwait(false);
            return true;
        }

        private static bool Matches(string queueUrl, string queueName)
            => queueUrl.Substring(queueUrl.LastIndexOf("/", StringComparison.Ordinal) + 1)
                .Equals(queueName, StringComparison.OrdinalIgnoreCase);

        public virtual async Task<bool> CreateAsync(SqsBasicConfiguration queueConfig, int attempt = 0)
        {
            try
            {
                var queueResponse = await Client.CreateQueueAsync(QueueName).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(queueResponse?.QueueUrl))
                {
                    Uri = new Uri(queueResponse.QueueUrl);
                    await Client.SetQueueAttributesAsync(queueResponse.QueueUrl, GetCreateQueueAttributes(queueConfig)).ConfigureAwait(false);
                    await SetQueuePropertiesAsync().ConfigureAwait(false);

                    _log.LogInformation("Created {QueueName} on Arn: {Arn}", QueueName, Arn);
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    _log.LogInformation("Waiting to create Queue due to AWS time restriction. {QueueName}, {attemptCount}",
                        QueueName, attempt + 1);

                    await Task.Delay(60000).ConfigureAwait(false);
                    await CreateAsync(queueConfig, attempt + 1).ConfigureAwait(false);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    _log.LogError(0, ex, "Create Queue error {QueueName}", QueueName);
                    throw;
                }

                // If we're on a delete timeout, throw after 2 attempts.
                if (attempt >= 2)
                {
                    _log.LogError(0, ex, "Create Queue error, max retries exceeded for delay on {QueueName}", QueueName);
                    throw;
                }
            }

            _log.LogInformation("Failed to create {QueueName}", QueueName);
            return false;
        }

        protected abstract Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig);
    }
}
