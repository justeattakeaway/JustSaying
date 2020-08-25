using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SqsQueueByNameBase : SqsQueueBase
    {
        protected SqsQueueByNameBase(RegionEndpoint region, string queueName, IAmazonSQS client, ILoggerFactory loggerFactory)
            : base(region, client, loggerFactory)
        {
            QueueName = queueName;
        }

        public override async Task<bool> ExistsAsync()
        {
            if (string.IsNullOrWhiteSpace(QueueName))
            {
                return false;
            }

            GetQueueUrlResponse result;

            try
            {
                using (Logger.Time("Checking if queue '{QueueName}' exists", QueueName))
                {
                    result = await Client.GetQueueUrlAsync(QueueName).ConfigureAwait(false);
                }
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

        private static readonly TimeSpan CreateRetryDelay = TimeSpan.FromMinutes(1);

        public virtual async Task<bool> CreateAsync(SqsBasicConfiguration queueConfig, int attempt = 0)
        {
            // If we're on a delete timeout, throw after 3 attempts.
            const int maxAttempts = 3;

            try
            {
                var queueResponse = await Client.CreateQueueAsync(QueueName).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(queueResponse?.QueueUrl))
                {
                    Uri = new Uri(queueResponse.QueueUrl);
                    await Client.SetQueueAttributesAsync(queueResponse.QueueUrl, GetCreateQueueAttributes(queueConfig)).ConfigureAwait(false);
                    await SetQueuePropertiesAsync().ConfigureAwait(false);

                    Logger.LogInformation("Created queue '{QueueName}' with ARN '{Arn}'.", QueueName, Arn);
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    if (attempt >= (maxAttempts - 1))
                    {
                        Logger.LogError(
                            ex,
                            "Error trying to create queue '{QueueName}'. Maximum retries of {MaxAttempts} exceeded for delay {Delay}.",
                            QueueName, maxAttempts,
                            CreateRetryDelay);

                        throw;
                    }

                    // Ensure we wait for queue delete timeout to expire.
                    Logger.LogInformation(
                        "Waiting to create queue '{QueueName}' for {Delay}, due to AWS time restriction. Attempt number {AttemptCount} of {MaxAttempts}.",
                        QueueName,
                        CreateRetryDelay,
                        attempt + 1,
                        maxAttempts);

                    await Task.Delay(CreateRetryDelay).ConfigureAwait(false);
                    await CreateAsync(queueConfig, attempt + 1).ConfigureAwait(false);
                }
                else
                {
                    // Throw all errors which are not delete timeout related.
                    Logger.LogError(ex, "Error trying to create queue '{QueueName}'.", QueueName);
                    throw;
                }
            }

            Logger.LogWarning("Failed to create queue '{QueueName}'.", QueueName);
            return false;
        }

        protected abstract Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig);
    }
}
