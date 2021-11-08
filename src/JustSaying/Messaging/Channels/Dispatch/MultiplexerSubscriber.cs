using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal class MultiplexerSubscriber : IMultiplexerSubscriber
    {
        private IAsyncEnumerable<IQueueMessageContext> _messageSource;
        private readonly IMessageDispatcher _dispatcher;
        private readonly string _subscriberId;
        private readonly ILogger<MultiplexerSubscriber> _logger;

        public MultiplexerSubscriber(
            IMessageDispatcher dispatcher,
            string subscriberId,
            ILogger<MultiplexerSubscriber> logger)
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

            _logger.LogTrace("Starting up {StartupType} with Id {SubscriberId}",
                nameof(MultiplexerSubscriber),
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
