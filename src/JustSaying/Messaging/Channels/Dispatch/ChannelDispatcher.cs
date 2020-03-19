using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal class ChannelDispatcher : IChannelDispatcher
    {
        private IAsyncEnumerable<IQueueMessageContext> _messageSource;
        private readonly IMessageDispatcher _dispatcher;

        public ChannelDispatcher(IMessageDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void DispatchFrom(IAsyncEnumerable<IQueueMessageContext> messageSource)
        {
            _messageSource = messageSource;
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
