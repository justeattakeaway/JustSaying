using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.ConsumerGroups;

namespace JustSaying.Messaging.Channels.Receive
{
    internal interface IReceiveBufferFactory
    {
        IMessageReceiveBuffer CreateBuffer(ISqsQueue queue, ConsumerGroupSettings consumerGroupSettings);
    }
}
