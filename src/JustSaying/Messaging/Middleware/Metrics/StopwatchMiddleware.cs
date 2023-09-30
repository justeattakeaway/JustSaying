using System.Diagnostics;
using JustSaying.Messaging.Monitoring;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

/// <summary>
/// This middleware measures the handler's execution duration and reports the results to an <see cref="IMessageMonitor"/>.
/// </summary>
/// <remarks>
/// Creates an instance of a <see cref="StopwatchMiddleware"/> that will report results to an
/// <see cref="IMessageMonitor"/>.
/// </remarks>
/// <param name="monitor">An <see cref="IMessageMonitor"/> to report results to.</param>
/// <param name="handlerType">The type of the handler that results should be reported against.</param>
public sealed class StopwatchMiddleware(IMessageMonitor monitor, Type handlerType) : MiddlewareBase<HandleMessageContext, bool>
{
    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        var watch = Stopwatch.StartNew();

        try
        {
            using (monitor.MeasureDispatch())
            {
                return await func(stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            watch.Stop();
            monitor.HandlerExecutionTime(handlerType, context.MessageType, watch.Elapsed);
        }
    }
}
