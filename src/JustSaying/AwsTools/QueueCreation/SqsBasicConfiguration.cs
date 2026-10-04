using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
using JustSaying.Naming;

namespace JustSaying.AwsTools.QueueCreation;

public class SqsBasicConfiguration
{
    // The ranges SQS accepts for a queue's VisibilityTimeout and its redrive policy's maxReceiveCount.
    private static readonly TimeSpan MaximumVisibilityTimeout = TimeSpan.FromHours(12);
    private const int MinimumRetryCount = 1;
    private const int MaximumRetryCount = 1000;

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

        if (VisibilityTimeout <= TimeSpan.Zero ||
            VisibilityTimeout > MaximumVisibilityTimeout)
        {
            throw new ConfigurationErrorsException(
                $"Invalid configuration. {nameof(VisibilityTimeout)} must be greater than zero and at most {MaximumVisibilityTimeout}.");
        }

        if (!ErrorQueueOptOut &&
            (RetryCountBeforeSendingToErrorQueue < MinimumRetryCount || RetryCountBeforeSendingToErrorQueue > MaximumRetryCount))
        {
            throw new ConfigurationErrorsException(
                $"Invalid configuration. {nameof(RetryCountBeforeSendingToErrorQueue)} must be between {MinimumRetryCount} and {MaximumRetryCount}.");
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

        if (ResourceNameValidator.GetQueueNameError(QueueName, hasErrorQueue: !ErrorQueueOptOut) is { } queueNameError)
        {
            throw new ConfigurationErrorsException($"Invalid configuration. {queueNameError}");
        }

        OnValidating();
    }

    /// <summary>
    /// Validates the configuration, naming the registration it belongs to in any error.
    /// </summary>
    /// <param name="registration">A description of the registration, for example <c>queue subscription for 'Order' to queue 'orders'</c>.</param>
    internal void Validate(string registration)
    {
        try
        {
            Validate();
        }
        catch (ConfigurationErrorsException ex)
        {
            throw new ConfigurationErrorsException($"{ex.Message} (in the {registration})", ex);
        }
    }

    /// <summary>
    /// Allows a derived class to implement custom validation.
    /// </summary>
    protected virtual void OnValidating()
    {
    }
}
