namespace JustSaying.Messaging.Channels.Receive;

/// <summary>
/// Allows stopping and starting the receiving of messages in all instances of the <see cref="MessageReceiveBuffer"/>
/// </summary>
public interface IMessageReceiveStatusSetter
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
    /// Value of <see cref="MessageReceiveStatus"/> which indicates receiving status
    /// </summary>
    MessageReceiveStatus Status { get; }
}
