using System.Diagnostics;
using JustSaying.Messaging.Monitoring;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware
{
    /// <summary>
    /// This middleware measures the handler's execution duration and reports the results to an <see cref="IMessageMonitor"/>.
    /// </summary>
    public sealed class StopwatchMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageMonitor _monitor;
        private readonly Type _handlerType;

        /// <summary>
        /// Creates an instance of a <see cref="StopwatchMiddleware"/> that will report results to an
        /// <see cref="IMessageMonitor"/>.
        /// </summary>
        /// <param name="monitor">An <see cref="IMessageMonitor"/> to report results to.</param>
        /// <param name="handlerType">The type of the handler that results should be reported against.</param>
        public StopwatchMiddleware(IMessageMonitor monitor, Type handlerType)
        {
            _monitor = monitor;
            _handlerType = handlerType;
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            var watch = Stopwatch.StartNew();

            try
            {
                using (_monitor.MeasureDispatch())
                {
                    return await func(stoppingToken).ConfigureAwait(false);
                }
            }
            finally
            {
                watch.Stop();
                _monitor.HandlerExecutionTime(_handlerType, context.MessageType, watch.Elapsed);
            }
        }
    }
}
