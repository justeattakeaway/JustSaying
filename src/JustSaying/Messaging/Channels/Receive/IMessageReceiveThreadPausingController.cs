namespace JustSaying.Messaging.Channels.Receive;

internal interface IMessageReceiveThreadPausingController
{
    /// <summary>
    /// Makes threads wait until Start method is called on <see cref="MessageReceiveController"/> or the <see cref="stoppingToken"/> is canceled.
    /// </summary>
    /// <param name="stoppingToken">A <see cref="CancellationToken"/> that can cancel the wait.</param>
    void PauseThreads(CancellationToken stoppingToken);
}
