namespace JustSaying.Messaging.Channels.Receive;

/// <summary>
/// Allows stopping and starting the receiving of messages in all instances of the <see cref="MessageReceiveBuffer"/>
/// </summary>
public interface IMessageReceiveController
{
    /// <summary>
    /// Sets status to stop receiving
    /// </summary>
    void Stop();

    /// <summary>
    /// Sets status to start receiving
    /// </summary>
    void Start();

    /// <summary>
    /// Indicates if receiving should be stopped
    /// </summary>
    bool ShouldStopReceiving { get; }
}
