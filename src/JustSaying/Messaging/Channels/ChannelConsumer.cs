using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling.Dispatch;

namespace JustSaying.Messaging.Channels
{
    internal class ChannelConsumer : IChannelConsumer
    {
        private IAsyncEnumerable<IQueueMessageContext> _messageSource;
        private readonly IMessageDispatcher _dispatcher;

        public ChannelConsumer(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IChannelConsumer ConsumeFrom(IAsyncEnumerable<IQueueMessageContext> messageSource)
        {
            _messageSource = messageSource;
            return this;
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            await foreach (var messageContext in _messageSource.WithCancellation(stoppingToken))
            {
                stoppingToken.ThrowIfCancellationRequested();

                await _dispatcher.DispatchMessageAsync(messageContext, stoppingToken)
                        .ConfigureAwait(false);
            }
        }
    }
}
