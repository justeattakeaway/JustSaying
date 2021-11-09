using JustSaying.Naming;

namespace JustSaying.AwsTools.QueueCreation;

public class SqsReadConfiguration : SqsBasicConfiguration
{
    public SqsReadConfiguration(SubscriptionType subscriptionType)
    {
        SubscriptionType = subscriptionType;
        MessageRetention = JustSayingConstants.DefaultRetentionPeriod;
        ErrorQueueRetentionPeriod = JustSayingConstants.MaximumRetentionPeriod;
        VisibilityTimeout = JustSayingConstants.DefaultVisibilityTimeout;
        RetryCountBeforeSendingToErrorQueue = JustSayingConstants.DefaultHandlerRetryCount;
    }

    public SubscriptionType SubscriptionType { get; }

    public string TopicName { get; set; }
    public string PublishEndpoint { get; set; }
    public Dictionary<string, string> Tags { get; set; }
    public string TopicSourceAccount { get; set; }
    public string FilterPolicy { get; set; }
    public string SubscriptionGroupName { get; set; }

    public void ApplyTopicNamingConvention<T>(ITopicNamingConvention namingConvention)
    {
        TopicName = namingConvention.Apply<T>(TopicName);
    }

    protected override void OnValidating()
    {
        if (SubscriptionType == SubscriptionType.ToTopic)
        {
            if (string.IsNullOrWhiteSpace(TopicName))
            {
                throw new ConfigurationErrorsException(
                    "Invalid configuration. Topic name must be provided.");
            }

            if (PublishEndpoint == null)
            {
                throw new ConfigurationErrorsException("You must provide a value for PublishEndpoint.");
            }
        }

        if (string.IsNullOrWhiteSpace(SubscriptionGroupName))
        {
            throw new ConfigurationErrorsException("You must provide a name for the subscription group");
        }
    }
}