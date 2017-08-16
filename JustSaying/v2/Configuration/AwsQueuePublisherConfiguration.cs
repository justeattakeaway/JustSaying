using JustSaying.AwsTools;

namespace JustSaying.v2.Configuration
{
    public interface IAwsQueuePublisherConfiguration : IAwsQueueNameConfiguration
    {
        int RetryCountBeforeSendingToErrorQueue { get; set; }
        int ErrorQueueRetentionPeriodSeconds { get; set; }
        int MessageRetentionSeconds { get; set; }
        int VisibilityTimeoutSeconds { get; set; }
        int DeliveryDelaySeconds { get; set; }
        bool ErrorQueueOptOut { get; set; }
    }

    public class AwsQueuePublisherConfiguration : IAwsQueuePublisherConfiguration
    {
        public string QueueNameOverride { get; set; }
        public int RetryCountBeforeSendingToErrorQueue { get; set; }
        public int ErrorQueueRetentionPeriodSeconds { get; set; }
        public int MessageRetentionSeconds { get; set; }
        public int VisibilityTimeoutSeconds { get; set; }
        public int DeliveryDelaySeconds { get; set; }
        public bool ErrorQueueOptOut { get; set; }

        public AwsQueuePublisherConfiguration()
        {
            MessageRetentionSeconds = JustSayingConstants.DEFAULT_RETENTION_PERIOD;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MAXIMUM_RETENTION_PERIOD;
            VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT;
            DeliveryDelaySeconds = JustSayingConstants.MINIMUM_DELIVERY_DELAY;
        }
    }
}