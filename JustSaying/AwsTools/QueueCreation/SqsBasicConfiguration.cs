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
            MessageRetentionSeconds = JustSayingConstants.DefaultRetentionPeriod;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MaximumRetentionPeriod;
            VisibilityTimeoutSeconds = JustSayingConstants.DefaultVisibilityTimeout;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
            DeliveryDelaySeconds = JustSayingConstants.MinimumDeliveryDelay;
        }

        public virtual void Validate()
        {
            if (MessageRetentionSeconds < JustSayingConstants.MinimumRetentionPeriod ||
                MessageRetentionSeconds > JustSayingConstants.MaximumRetentionPeriod)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. MessageRetentionSeconds must be between {JustSayingConstants.MinimumRetentionPeriod} and {JustSayingConstants.MaximumRetentionPeriod}.");
            }

            if (ErrorQueueRetentionPeriodSeconds < JustSayingConstants.MinimumRetentionPeriod ||
                ErrorQueueRetentionPeriodSeconds > JustSayingConstants.MaximumRetentionPeriod)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. ErrorQueueRetentionPeriodSeconds must be between {JustSayingConstants.MinimumRetentionPeriod} and {JustSayingConstants.MaximumRetentionPeriod}.");
            }

            if (DeliveryDelaySeconds < JustSayingConstants.MinimumDeliveryDelay ||
                DeliveryDelaySeconds > JustSayingConstants.MaximumDeliveryDelay)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. DeliveryDelaySeconds must be between {JustSayingConstants.MinimumDeliveryDelay} and {JustSayingConstants.MaximumDeliveryDelay}.");
            }
        }
    }
}
