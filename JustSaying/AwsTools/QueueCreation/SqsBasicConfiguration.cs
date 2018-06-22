namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsBasicConfiguration
    {
        public int MessageRetentionSeconds { get; set; }
        public int ErrorQueueRetentionPeriodSeconds { get; set; }
        public int VisibilityTimeoutSeconds { get; set; }
        public int DeliveryDelaySeconds { get; set; }
        public int RetryCountBeforeSendingToErrorQueue { get; set; }
        public bool ErrorQueueOptOut { get; set; }
        public ServerSideEncryption ServerSideEncryption { get; set; }

        public SqsBasicConfiguration()
        {
            MessageRetentionSeconds = JustSayingConstants.DEFAULT_RETENTION_PERIOD;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD;
            VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT;
            DeliveryDelaySeconds = JustSayingConstants.MINIMUM_DELIVERY_DELAY;
        }

        public virtual void Validate()
        {
            if (MessageRetentionSeconds < JustSayingConstants.MINIMUM_RETENTION_PERIOD ||
                MessageRetentionSeconds > JustSayingConstants.MAXIMUM_RETENTION_PERIOD)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. MessageRetentionSeconds must be between {JustSayingConstants.MINIMUM_RETENTION_PERIOD} and {JustSayingConstants.MAXIMUM_RETENTION_PERIOD}.");
            }

            if (ErrorQueueRetentionPeriodSeconds < JustSayingConstants.MINIMUM_RETENTION_PERIOD ||
                ErrorQueueRetentionPeriodSeconds > JustSayingConstants.MAXIMUM_RETENTION_PERIOD)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. ErrorQueueRetentionPeriodSeconds must be between {JustSayingConstants.MINIMUM_RETENTION_PERIOD} and {JustSayingConstants.MAXIMUM_RETENTION_PERIOD}.");
            }

            if (DeliveryDelaySeconds < JustSayingConstants.MINIMUM_DELIVERY_DELAY ||
                DeliveryDelaySeconds > JustSayingConstants.MAXIMUM_DELIVERY_DELAY)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. DeliveryDelaySeconds must be between {JustSayingConstants.MINIMUM_DELIVERY_DELAY} and {JustSayingConstants.MAXIMUM_DELIVERY_DELAY}.");
            }
        }
    }
}
