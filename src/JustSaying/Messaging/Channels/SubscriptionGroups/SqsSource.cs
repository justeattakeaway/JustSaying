using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

public sealed class SqsSource : IMessageSource
{
    public ISqsQueue SqsQueue { get; set; }
    public IInboundMessageConverter MessageConverter { get; set; }
    
    string IMessageSource.Name => SqsQueue?.QueueName ?? string.Empty;
}
