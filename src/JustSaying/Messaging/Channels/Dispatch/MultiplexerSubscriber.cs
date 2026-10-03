using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Dispatch;

internal class MultiplexerSubscriber(
    IMessageDispatcher dispatcher,
    string subscriberId,
    ILogger<MultiplexerSubscriber> logger,
    IRateLimiter rateLimiter = null) : IMultiplexerSubscriber
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

            if (rateLimiter != null)
            {
                await rateLimiter.WaitAsync(stoppingToken).ConfigureAwait(false);
            }

            try
            {
                await dispatcher.DispatchMessageAsync(messageContext, stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One bad message must never stop this subscriber: the message is left on the queue
                // for its visibility timeout and redrive policy, and we carry on with the next one.
                logger.LogError(
                    ex,
                    "Subscriber {SubscriberId} failed to dispatch message with Id '{MessageId}' from queue '{QueueName}'. The message has been left on the queue.",
                    subscriberId,
                    messageContext.Message?.MessageId,
                    messageContext.QueueName);
            }
        }
    }
}
