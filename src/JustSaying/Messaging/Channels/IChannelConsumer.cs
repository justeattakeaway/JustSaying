using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    internal interface IChannelConsumer
    {
        Task Start();
        void ConsumeFrom(IAsyncEnumerable<IQueueMessageContext> messageSource);
    }

    internal class ChannelConsumer : IChannelConsumer
    {
        private IAsyncEnumerable<IQueueMessageContext> _messageSource;
        private readonly IMessageDispatcher _dispatcher;

        public ChannelConsumer(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void ConsumeFrom(IAsyncEnumerable<IQueueMessageContext> messageSource)
        {
            _messageSource = messageSource;
        }

        public async Task Start()
        {
            await foreach ( var messageContext in _messageSource)
            {
                await _dispatcher.DispatchMessage(messageContext, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
