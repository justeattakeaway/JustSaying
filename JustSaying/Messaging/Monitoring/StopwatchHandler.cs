using System;
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
        private readonly Type _handlerType;

        public StopwatchHandler(IHandlerAsync<T> inner, IMeasureHandlerExecutionTime monitoring)
        {
            _inner = inner;
            _monitoring = monitoring;
            _handlerType = _inner.GetType();
        }

        public async Task<bool> Handle(T message)
        {
            var stopwatch = Stopwatch.StartNew();

            bool result = await _inner.Handle(message).ConfigureAwait(false);

            stopwatch.Stop();

            _monitoring.HandlerExecutionTime(_handlerType, message.GetType(), stopwatch.Elapsed);

            return result;
        }
    }
}
