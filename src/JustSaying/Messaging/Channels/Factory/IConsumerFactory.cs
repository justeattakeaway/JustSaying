using JustSaying.AwsTools.MessageHandling.Dispatch;

namespace JustSaying.Messaging.Channels.Factory
{
    internal interface IConsumerFactory
    {
        IChannelConsumer Create();
    }

    internal class ConsumerFactory : IConsumerFactory
    {
        private readonly IMessageDispatcher _dispatcher;

        public ConsumerFactory(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IChannelConsumer Create()
        {
            return new ChannelConsumer(_dispatcher);
        }
    }
}
