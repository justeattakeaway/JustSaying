using JustSaying.AwsTools.MessageHandling.Dispatch;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal class MultiplexerSubscriberFactory : IMultiplexerSubscriberFactory
    {
        private readonly IMessageDispatcher _dispatcher;

        public MultiplexerSubscriberFactory(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IMultiplexerSubscriber Create()
        {
            return new MultiplexerSubscriber(_dispatcher);
        }
    }
}
