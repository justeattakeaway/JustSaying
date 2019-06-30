using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.Monitoring
{
    public class StopwatchHandler<T> : IHandlerAsync<T>, ICancellableHandlerAsync<T>
        where T : Message
    {
        private readonly Type _handlerType;
        private readonly Func<T, CancellationToken, Task<bool>> _inner;
        private readonly IMeasureHandlerExecutionTime _monitoring;

        public StopwatchHandler(IHandlerAsync<T> inner, IMeasureHandlerExecutionTime monitoring)
        {
            _handlerType = inner.GetType();
            _monitoring = monitoring;

            if (inner is ICancellableHandlerAsync<T> cancellable)
            {
                _inner = cancellable.HandleAsync;
            }
            else
            {
                _inner = async (message, _) => await inner.Handle(message).ConfigureAwait(false);
            }
        }

        public async Task<bool> Handle(T message)
        {
            return await HandleAsync(message, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> HandleAsync(T message, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            bool result = await _inner(message, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            _monitoring.HandlerExecutionTime(_handlerType, message.GetType(), stopwatch.Elapsed);

            return result;
        }
    }
}
