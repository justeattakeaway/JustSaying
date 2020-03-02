using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Monitoring
{
    public sealed class Operation : IDisposable
    {
        private readonly IMessageMonitor _messageMonitor;
        private readonly Action<TimeSpan, IMessageMonitor> _onComplete;
        private readonly Stopwatch _stopWatch;

        internal Operation(IMessageMonitor messageMonitor, Action<TimeSpan, IMessageMonitor> onComplete)
        {
            _messageMonitor = messageMonitor ?? throw new ArgumentNullException(nameof(messageMonitor));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));

            _stopWatch = new Stopwatch();
            _stopWatch.Start();
        }

        public void Dispose()
        {
            _onComplete(_stopWatch.Elapsed, _messageMonitor);
        }
    }
}
