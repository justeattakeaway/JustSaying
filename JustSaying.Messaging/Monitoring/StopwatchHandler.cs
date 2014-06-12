using System.Diagnostics;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Messages;

namespace JustSaying.Messaging.Monitoring
{
    public class StopwatchHandler<T> : IHandler<T> where T : Message
    {
        private readonly IHandler<T> _inner;
        private readonly IMeasureHandlerExecutionTime _monitoring;

        public StopwatchHandler(IHandler<T> inner, IMeasureHandlerExecutionTime monitoring)
        {
            _inner = inner;
            _monitoring = monitoring;
        }

        public bool Handle(T message)
        {
            var watch = Stopwatch.StartNew();
            var result = _inner.Handle(message);
            watch.Stop();
            _monitoring.HandlerExecutionTime(GetType().Name.ToLower(), message.GetType().Name.ToLower(),
                watch.Elapsed);
            return result;
        }
    }
}