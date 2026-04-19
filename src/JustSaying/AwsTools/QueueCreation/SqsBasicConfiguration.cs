using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
using JustSaying.Naming;

namespace JustSaying.AwsTools.QueueCreation;

public class SqsBasicConfiguration
{
    public TimeSpan MessageRetention { get; set; } = JustSayingConstants.DefaultRetentionPeriod;
    public TimeSpan ErrorQueueRetentionPeriod { get; set; } = JustSayingConstants.MaximumRetentionPeriod;
    public TimeSpan VisibilityTimeout { get; set; } = JustSayingConstants.DefaultVisibilityTimeout;
    public TimeSpan DeliveryDelay { get; set; } = JustSayingConstants.MinimumDeliveryDelay;
    public int RetryCountBeforeSendingToErrorQueue { get; set; } = JustSayingConstants.DefaultHandlerRetryCount;
    public bool ErrorQueueOptOut { get; set; }
    public ServerSideEncryption ServerSideEncryption { get; set; }
    public bool IsRawMessage { get; set; }
    public string QueueName { get; set; }

    public void ApplyQueueNamingConvention<T>(IQueueNamingConvention namingConvention)
    {
        QueueName = namingConvention.Apply<T>(QueueName);
    }

    public void Validate()
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

        if (ServerSideEncryption != null)
        {
            if (ServerSideEncryption.KmsDataKeyReusePeriod > TimeSpan.FromHours(24) ||
                ServerSideEncryption.KmsDataKeyReusePeriod < TimeSpan.FromSeconds(60))
            {
                throw new ConfigurationErrorsException(
                    $"Invalid configuration. {nameof(ServerSideEncryption.KmsDataKeyReusePeriod)} must be between 1 minute and 24 hours.");
            }
        }

        if (string.IsNullOrWhiteSpace(QueueName))
        {
            throw new ConfigurationErrorsException("Invalid configuration. QueueName must be provided.");
        }

        OnValidating();
    }

    /// <summary>
    /// Allows a derived class to implement custom validation.
    /// </summary>
    protected virtual void OnValidating()
    {
    }
}
