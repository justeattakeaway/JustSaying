using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.SubscriptionGroups;

namespace JustSaying.Messaging.Channels.Receive
{
    internal interface IReceiveBufferFactory
    {
        IMessageReceiveBuffer CreateBuffer(ISqsQueue queue, SubscriptionGroupSettings subscriptionGroupSettings);
    }
}
