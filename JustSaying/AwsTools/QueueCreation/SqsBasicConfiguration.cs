using System;

namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsBasicConfiguration
    {
        public TimeSpan MessageRetention { get; set; }
        public TimeSpan ErrorQueueRetentionPeriod { get; set; }
        public TimeSpan VisibilityTimeout { get; set; }
        public TimeSpan DeliveryDelay { get; set; }
        public int RetryCountBeforeSendingToErrorQueue { get; set; }
        public bool ErrorQueueOptOut { get; set; }
        public ServerSideEncryption ServerSideEncryption { get; set; }

        public SqsBasicConfiguration()
        {
            MessageRetention = JustSayingConstants.DefaultRetentionPeriod;
            ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod;
            VisibilityTimeout = JustSayingConstants.DefaultVisibilityTimeout;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
            DeliveryDelay = JustSayingConstants.MinimumDeliveryDelay;
        }

        public virtual void Validate()
        {
            if (MessageRetention < JustSayingConstants.MinimumRetentionPeriod ||
                MessageRetention > JustSayingConstants.MaximumRetentionPeriod)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. {nameof(MessageRetention)} must be between {JustSayingConstants.MinimumRetentionPeriod} and {JustSayingConstants.MaximumRetentionPeriod}.");
            }

            if (ErrorQueueRetentionPeriod < JustSayingConstants.MinimumRetentionPeriod ||
                ErrorQueueRetentionPeriod > JustSayingConstants.MaximumRetentionPeriod)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. {nameof(ErrorQueueRetentionPeriod)} must be between {JustSayingConstants.MinimumRetentionPeriod} and {JustSayingConstants.MaximumRetentionPeriod}.");
            }

            if (DeliveryDelay < JustSayingConstants.MinimumDeliveryDelay ||
                DeliveryDelay > JustSayingConstants.MaximumDeliveryDelay)
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. {nameof(DeliveryDelay)} must be between {JustSayingConstants.MinimumDeliveryDelay} and {JustSayingConstants.MaximumDeliveryDelay}.");
            }
        }
    }
}
