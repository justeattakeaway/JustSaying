using System.Diagnostics;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.Monitoring
{
    public class StopwatchHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly IHandlerAsync<T> _inner;
        private readonly IMeasureHandlerExecutionTime _monitoring;

        public StopwatchHandler(IHandlerAsync<T> inner, IMeasureHandlerExecutionTime monitoring)
        {
            _inner = inner;
            _monitoring = monitoring;
        }

        public async Task<bool> Handle(T message)
        {
            var watch = Stopwatch.StartNew();
            var result = await _inner.Handle(message);

            watch.Stop();
            _monitoring.HandlerExecutionTime(GetType().Name.ToLower(), 
                message.GetType().Name.ToLower(), watch.Elapsed);
            return result;
        }
    }
}