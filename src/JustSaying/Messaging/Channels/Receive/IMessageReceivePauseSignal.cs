namespace JustSaying.Messaging.Channels.Receive;

/// <summary>
/// Allows stopping and starting the receiving of messages in all instances of the <see cref="MessageReceiveBuffer"/>
/// </summary>
public interface IMessageReceivePauseSignal
{
    /// <summary>
    /// Sets status to pause receiving
    /// </summary>
    void Pause();

    /// <summary>
    /// Sets status to start receiving
    /// </summary>
    void Start();

    /// <summary>
    /// Indicates receiving of messages is paused
    /// </summary>
    bool IsPaused { get; }
}
