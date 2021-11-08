using JustSaying.Messaging.Channels.Context;

namespace JustSaying.Messaging.Channels.Dispatch;

/// <summary>
/// A cancellable component that can listen to an <see cref="IAsyncEnumerable{IQueueMessageContext}"/>
/// </summary>
internal interface IMultiplexerSubscriber
{
    /// <summary>
    /// Begins consuming from the source passed to <see cref="Subscribe"/> until the cancellation token is canceled.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token that cancels the subscriber and stops reading
    /// from the stream passed to <see cref="Subscribe"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the stream passed to <see cref="Subscribe"/> is closed or the
    /// <see cref="stoppingToken"/> is canceled.</returns>
    Task RunAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Provides the <see cref="IMultiplexerSubscriber"/> with a stream to read from.
    /// </summary>
    /// <param name="messageSource">The source of messages to subscribe to.</param>
    void Subscribe(IAsyncEnumerable<IQueueMessageContext> messageSource);
}