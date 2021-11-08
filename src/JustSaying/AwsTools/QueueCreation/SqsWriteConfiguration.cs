namespace JustSaying.AwsTools.QueueCreation;

public class SqsWriteConfiguration : SqsBasicConfiguration
{
    public SqsWriteConfiguration()
    {
        MessageRetention = JustSayingConstants.DefaultRetentionPeriod;
        ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod;
        VisibilityTimeout = JustSayingConstants.DefaultVisibilityTimeout;
        RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
    }
}