namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsWriteConfiguration : SqsBasicConfiguration
    {
        public SqsWriteConfiguration()
        {
            MessageRetention = JustSayingConstants.DefaultRetentionPeriod;
            ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod;
            VisibilityTimeout = JustSayingConstants.DefaultVisibilityTimeout;
            RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
        }

        public string QueueName { get; set; }

        protected override void ValidateCustom()
        {
            if (string.IsNullOrWhiteSpace(QueueName))
            {
                throw new ConfigurationErrorsException("Invalid configuration. QueueName must be provided.");
            }
        }
    }
}
