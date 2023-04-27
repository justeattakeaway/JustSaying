namespace JustSaying.Messaging.Channels.Receive;

public class MessageReceiveController : IMessageReceiveController
{
    private bool _stopped;

    public void Stop()
    {
        _stopped = true;
    }

    public void Start()
    {
        _stopped = false;
    }

    public bool Stopped()
    {
        return _stopped;
    }
}
