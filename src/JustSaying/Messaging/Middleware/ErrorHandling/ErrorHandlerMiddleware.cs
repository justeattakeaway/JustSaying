using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.ErrorHandling
{
    /// <summary>
    /// A middleware that calls members of an <see cref="IMessageMonitor"/> at correct times during message processing.
    /// This middleware calls HandleException(Type messageType), HandleError(Exception ex, Amazon.SQS.Model.Message message),
    /// and Handled(JustSaying.Models.Message message).
    /// </summary>
    public class ErrorHandlerMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageMonitor _monitor;

        public ErrorHandlerMiddleware(IMessageMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            try
            {
                return await func(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _monitor.HandleException(context.MessageType);
                _monitor.HandleError(e, context.RawMessage);

                context.SetException(e);
                return false;
            }
            finally
            {
                _monitor.Handled(context.Message);
            }
        }
    }
}
