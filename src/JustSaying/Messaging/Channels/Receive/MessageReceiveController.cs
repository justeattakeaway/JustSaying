namespace JustSaying.Messaging.Channels.Receive;

public class MessageReceiveController : IMessageReceiveController
{
    public void Stop()
    {
        ShouldStopReceiving = true;
    }

    public void Start()
    {
        ShouldStopReceiving = false;
    }

    public bool ShouldStopReceiving { get; private set; }
}
