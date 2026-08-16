namespace JustSaying.Messaging.Channels.Receive;

/// <summary>
/// Allows pausing and resuming the receipt of messages in all instances of the <see cref="MessageReceiveBuffer"/>
/// </summary>
public interface IMessageReceivePauseSignal
{
    /// <summary>
    /// Sets status to pause receiving
    /// </summary>
    /// <remarks>
    /// Pausing cancels any in-flight <c>ReceiveMessage</c> call. Messages that SQS was in the
    /// process of serving to that call may remain invisible until their visibility timeout
    /// expires, so occasionally a message is delayed rather than being immediately received
    /// by another consumer.
    /// </remarks>
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
