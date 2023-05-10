namespace JustSaying.Messaging.Channels.Receive;

public class MessageReceiveStatusSetter : IMessageReceiveStatusSetter
{
    public void Stop()
    {
        Status = MessageReceiveStatus.NotReceiving;
    }

    public void Start()
    {
        Status = MessageReceiveStatus.Receiving;
    }

    public MessageReceiveStatus Status { get; private set; }
}
