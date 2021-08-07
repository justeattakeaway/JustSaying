using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.Backoff
{
    public class BackoffMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageBackoffStrategy _backoffStrategy;
        private readonly ILogger<BackoffMiddleware> _logger;
        private readonly IMessageMonitor _monitor;

        public BackoffMiddleware(IMessageBackoffStrategy backoffStrategy, ILoggerFactory loggerFactory, IMessageMonitor monitor)
        {
            _backoffStrategy = backoffStrategy;
            _monitor = monitor;
            _logger = loggerFactory.CreateLogger<BackoffMiddleware>();
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            bool handlingSucceeded = false;
            try
            {
                handlingSucceeded = await func(stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                if (!handlingSucceeded)
                {
                    await TryUpdateVisibilityTimeout(context, stoppingToken);
                }
            }

            return handlingSucceeded;
        }

        private async Task TryUpdateVisibilityTimeout(HandleMessageContext context, CancellationToken stoppingToken)
        {
            if (TryGetApproxReceiveCount(context.RawMessage.Attributes, out int approxReceiveCount))
            {
                TimeSpan backoffDuration = _backoffStrategy.GetBackoffDuration(context.Message, approxReceiveCount, context.HandleException);
                try
                {
                    await context.VisibilityUpdater.UpdateMessageVisibilityTimeout(backoffDuration, stoppingToken).ConfigureAwait(false);
                }
                catch (AmazonServiceException ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to update message visibility timeout by {VisibilityTimeout} seconds for message with receipt handle '{ReceiptHandle}'.",
                        backoffDuration,
                        context.RawMessage.ReceiptHandle);

                    _monitor.HandleError(ex, context.RawMessage);
                }
            }
        }

        private static bool TryGetApproxReceiveCount(
            IDictionary<string, string> attributes,
            out int approxReceiveCount)
        {
            approxReceiveCount = 0;

            return attributes.TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount,
                    out string rawApproxReceiveCount) &&
                int.TryParse(rawApproxReceiveCount,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out approxReceiveCount);
        }
    }
}
