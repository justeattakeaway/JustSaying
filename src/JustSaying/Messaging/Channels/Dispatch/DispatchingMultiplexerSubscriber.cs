using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal class DispatchingMultiplexerSubscriber : IMultiplexerSubscriber
    {
        private IAsyncEnumerable<IQueueMessageContext> _messageSource;
        private readonly IMessageDispatcher _dispatcher;
        private readonly string _subscriberId;
        private readonly ILogger<DispatchingMultiplexerSubscriber> _logger;

        public DispatchingMultiplexerSubscriber(
            IMessageDispatcher dispatcher,
            string subscriberId,
            ILogger<DispatchingMultiplexerSubscriber> logger)
        {
            _dispatcher = dispatcher;
            _subscriberId = subscriberId;
            _logger = logger;
        }

        public void Subscribe(IAsyncEnumerable<IQueueMessageContext> messageSource)
        {
            _messageSource = messageSource;
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            _logger.LogDebug("Starting up {StartupType} {SubscriberId}",
                nameof(DispatchingMultiplexerSubscriber),
                _subscriberId);

            await foreach (IQueueMessageContext messageContext in _messageSource.WithCancellation(
                stoppingToken))
            {
                stoppingToken.ThrowIfCancellationRequested();

                await _dispatcher.DispatchMessageAsync(messageContext, stoppingToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
