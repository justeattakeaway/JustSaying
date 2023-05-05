namespace JustSaying.Messaging.Channels.Receive;

public class MessageReceiveController : IMessageReceiveController, IMessageReceiveThreadPausingController
{
    private readonly ManualResetEvent _manualResetEvent = new(false);

    public void Stop()
    {
        ShouldStopReceiving = true;
    }

    public void Start()
    {
        _manualResetEvent.Set();
        _manualResetEvent.Reset();
        ShouldStopReceiving = false;
    }

    public void PauseThreads(CancellationToken cancellationToken)
    {
        WaitHandle.WaitAny(new[] { _manualResetEvent, cancellationToken.WaitHandle });
    }

    public bool ShouldStopReceiving { get; private set; }
}
