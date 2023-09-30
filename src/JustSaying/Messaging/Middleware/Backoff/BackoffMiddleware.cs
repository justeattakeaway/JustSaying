using System.Globalization;
using Amazon.Runtime;
using Amazon.SQS;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.Backoff;

/// <summary>
/// Implements a middleware that will execute an <see cref="IMessageBackoffStrategy"/> to delay message redelivery when a handler returns false or throws.
/// </summary>
/// <remarks>
/// Constructs a <see cref="BackoffMiddleware"/> with a given backoff strategy and logger/monitor.
/// </remarks>
/// <param name="backoffStrategy">An <see cref="IMessageBackoffStrategy"/> to use to determine how long to delay message redelivery when a handler returns false or throws.</param>
/// <param name="loggerFactory">An <see cref="ILoggerFactory"/> to use when logging request failures.</param>
/// <param name="monitor">An <see cref="IMessageMonitor"/> to use when recording request failures.</param>
public sealed class BackoffMiddleware(IMessageBackoffStrategy backoffStrategy, ILoggerFactory loggerFactory, IMessageMonitor monitor) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly IMessageBackoffStrategy _backoffStrategy = backoffStrategy ?? throw new ArgumentNullException(nameof(backoffStrategy));
    private readonly ILogger<BackoffMiddleware> _logger = loggerFactory?.CreateLogger<BackoffMiddleware>() ??
                  throw new ArgumentNullException(nameof(loggerFactory));
    private readonly IMessageMonitor _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

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
        Dictionary<string, string> attributes,
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
