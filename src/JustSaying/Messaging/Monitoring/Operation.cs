using System.Diagnostics;

namespace JustSaying.Messaging.Monitoring;

internal sealed class Operation : IDisposable
{
    private readonly IMessageMonitor _messageMonitor;
    private readonly Action<TimeSpan, IMessageMonitor> _onComplete;
    private readonly Stopwatch _stopWatch;

    internal Operation(IMessageMonitor messageMonitor, Action<TimeSpan, IMessageMonitor> onComplete)
    {
        _messageMonitor = messageMonitor ?? throw new ArgumentNullException(nameof(messageMonitor));
        _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));

        _stopWatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopWatch.Stop();
        _onComplete(_stopWatch.Elapsed, _messageMonitor);
    }
}