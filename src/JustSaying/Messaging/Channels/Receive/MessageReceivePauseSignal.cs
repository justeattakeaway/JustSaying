namespace JustSaying.Messaging.Channels.Receive;

public sealed class MessageReceivePauseSignal : IMessageReceivePauseSignal
{
    public void Pause()
    {
        IsPaused = true;
    }

    public void Resume()
    {
        IsPaused = false;
    }

    public bool IsPaused { get; private set; }
}
