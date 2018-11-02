namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsWriteConfiguration : SqsBasicConfiguration
    {
        public SqsWriteConfiguration()
        {
            MessageRetentionSeconds = JustSayingConstants.DefaultRetentionPeriod;
            ErrorQueueRetentionPeriodSeconds = JustSayingConstants.MaximumRetentionPeriod;
            VisibilityTimeoutSeconds = JustSayingConstants.DefaultVisibilityTimeout;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
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
