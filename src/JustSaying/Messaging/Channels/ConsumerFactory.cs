using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    internal interface IConsumerFactory
    {
        IChannelConsumer CreateConsumer();
    }

    internal class ConsumerFactory : IConsumerFactory
    {
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly ILoggerFactory _loggerFactory;

        public ConsumerFactory(IMessageDispatcher messageDispatcher, ILoggerFactory loggerFactory)
        {
            _messageDispatcher = messageDispatcher;
            _loggerFactory = loggerFactory;
        }

        public IChannelConsumer CreateConsumer()
        {
            return new ChannelConsumer(_messageDispatcher, _loggerFactory);
        }
    }
}
