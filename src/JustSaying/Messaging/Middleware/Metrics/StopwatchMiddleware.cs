using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware.Handle;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.Messaging.Middleware.Metrics
{
    public class StopwatchMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageMonitor _monitor;
        private readonly Type _handlerType;

        public StopwatchMiddleware(IMessageMonitor monitor, Type handlerType)
        {
            _monitor = monitor;
            _handlerType = handlerType;
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            var watch = Stopwatch.StartNew();

            bool result = await func(stoppingToken).ConfigureAwait(false);

            watch.Stop();

            _monitor.HandlerExecutionTime(_handlerType, context.MessageType, watch.Elapsed);

            return result;
        }
    }
}
