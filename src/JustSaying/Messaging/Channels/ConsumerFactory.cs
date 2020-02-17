using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    internal interface IConsumerFactory
    {
        IChannelConsumer CreateConsumer();
    }

    internal class ConsumerFactory : IConsumerFactory
    {
        private readonly IMessageDispatcher _messageDispatcher;

        public ConsumerFactory(IMessageDispatcher messageDispatcher)
        {
            _messageDispatcher = messageDispatcher;
        }

        public IChannelConsumer CreateConsumer()
        {
            return new ChannelConsumer(_messageDispatcher);
        }
    }
}
