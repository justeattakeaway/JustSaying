using JustSaying.AwsTools.QueueCreation;

namespace JustSaying
{
    public interface INamingStrategy
    {
        string GetPublishEndpoint(IPublishConfiguration publishConfig, string messageType);
        string GetSubscriptionEndpoint(SqsReadConfiguration subscriptionConfig, string messageType);
    }
}