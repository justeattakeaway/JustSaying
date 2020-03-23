using JustSaying.AwsTools.MessageHandling.Dispatch;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal class ChannelConsumerFactory : IChannelConsumerFactory
    {
        private readonly IMessageDispatcher _dispatcher;

        public ChannelConsumerFactory(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IChannelConsumer Create()
        {
            return new ChannelConsumer(_dispatcher);
        }
    }
}
