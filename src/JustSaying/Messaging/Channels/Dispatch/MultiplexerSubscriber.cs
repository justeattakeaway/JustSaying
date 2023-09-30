using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Dispatch;

internal class MultiplexerSubscriber(
    IMessageDispatcher dispatcher,
    string subscriberId,
    ILogger<MultiplexerSubscriber> logger) : IMultiplexerSubscriber
{
    private IAsyncEnumerable<IQueueMessageContext> _messageSource;

    public void Subscribe(IAsyncEnumerable<IQueueMessageContext> messageSource)
    {
        _messageSource = messageSource;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        logger.LogTrace("Starting up {StartupType} with Id {SubscriberId}",
            nameof(MultiplexerSubscriber),
            subscriberId);

        await foreach (IQueueMessageContext messageContext in _messageSource.WithCancellation(
                           stoppingToken))
        {
            stoppingToken.ThrowIfCancellationRequested();

            await dispatcher.DispatchMessageAsync(messageContext, stoppingToken)
                .ConfigureAwait(false);
        }
    }
}
