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
    /// <summary>
    /// Implements a middleware that will execute an <see cref="IMessageBackoffStrategy"/> to delay message redelivery when a handler returns false or throws.
    /// </summary>
    public class BackoffMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageBackoffStrategy _backoffStrategy;
        private readonly ILogger<BackoffMiddleware> _logger;
        private readonly IMessageMonitor _monitor;

        /// <summary>
        /// Constructs a <see cref="BackoffMiddleware"/> with a given backoff strategy and logger/monitor.
        /// </summary>
        /// <param name="backoffStrategy">An <see cref="IMessageBackoffStrategy"/> to use to determine how long to delay message redelivery when a handler returns false or throws.</param>
        /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> to use when logging request failures.</param>
        /// <param name="monitor">An <see cref="IMessageMonitor"/> to use when recording request failures.</param>
        public BackoffMiddleware(IMessageBackoffStrategy backoffStrategy, ILoggerFactory loggerFactory, IMessageMonitor monitor)
        {
            _backoffStrategy = backoffStrategy ?? throw new ArgumentNullException(nameof(backoffStrategy));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _logger = loggerFactory?.CreateLogger<BackoffMiddleware>() ??
                throw new ArgumentNullException(nameof(loggerFactory));
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
            if (!TryGetApproxReceiveCount(context.RawMessage.Attributes, out int approxReceiveCount)) return;

            TimeSpan backoffDuration = _backoffStrategy.GetBackoffDuration(context.Message, approxReceiveCount, context.HandledException);
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
