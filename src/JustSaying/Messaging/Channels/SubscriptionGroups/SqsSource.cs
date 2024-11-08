using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

public sealed class SqsSource
{
    public ISqsQueue SqsQueue { get; set; }
    public IInboundMessageConverter MessageConverter { get; set; }
}
