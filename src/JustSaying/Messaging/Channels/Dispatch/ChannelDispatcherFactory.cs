using JustSaying.AwsTools.MessageHandling.Dispatch;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal class ChannelDispatcherFactory : IChannelDispatcherFactory
    {
        private readonly IMessageDispatcher _dispatcher;

        public ChannelDispatcherFactory(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IChannelDispatcher Create()
        {
            return new ChannelDispatcher(_dispatcher);
        }
    }
}
