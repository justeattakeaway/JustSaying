using System.Configuration;

namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsWriteConfiguration : SqsBasicConfiguration
    {
        public SqsWriteConfiguration()
        {
            MessageRetentionSeconds = JustSayingConstants.DEFAULT_RETENTION_PERIOD;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD;
            VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT;
        }

        public string QueueName { get; set; }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(QueueName))
            {
                throw new ConfigurationErrorsException("Invalid configuration. QueueName must be provided.");
            }
        }
    }
}