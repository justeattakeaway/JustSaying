namespace JustSaying.Messaging.Channels.Receive;

/// <summary>
/// Allows pausing and resuming the receipt of messages in all instances of the <see cref="MessageReceiveBuffer"/>
/// </summary>
public interface IMessageReceivePauseSignal
{
    /// <summary>
    /// Sets status to pause receiving
    /// </summary>
    void Pause();

    /// <summary>
    /// Sets status to resume receiving
    /// </summary>
    void Resume();

    /// <summary>
    /// Indicates receiving of messages is paused
    /// </summary>
    bool IsPaused { get; }
}
