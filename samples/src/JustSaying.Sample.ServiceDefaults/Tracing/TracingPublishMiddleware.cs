using System.Diagnostics;
using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using OpenTelemetry.Trace;

namespace JustSaying.Sample.ServiceDefaults.Tracing;

/// <summary>
/// Publish middleware that creates an Activity span for message publishing
/// and propagates W3C trace context via message attributes.
/// </summary>
public class TracingPublishMiddleware : MiddlewareBase<PublishContext, bool>
{
    private static readonly ActivitySource ActivitySource = new("JustSaying.MessagePublisher");

    protected override async Task<bool> RunInnerAsync(
        PublishContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        var isBatch = context.Messages != null;
        var messageType = isBatch
            ? context.Messages.FirstOrDefault()?.GetType().Name ?? "Message"
            : context.Message.GetType().Name;
        var activityName = isBatch ? $"publish batch {messageType}" : $"publish {messageType}";

        using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetTag("messaging.system", "aws_sns");
            activity.SetTag("messaging.operation", "publish");
            activity.SetTag("messaging.message.type", messageType);

            if (isBatch)
            {
                activity.SetTag("messaging.batch.message_count", context.Messages.Count);
            }

            // Add W3C trace context to message attributes
            var traceparent = $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}";
            context.Metadata.AddMessageAttribute(TraceContextKeys.TraceParent, traceparent);

            if (activity.TraceStateString != null)
            {
                context.Metadata.AddMessageAttribute(TraceContextKeys.TraceState, activity.TraceStateString);
            }

            // Add message ID for correlation (single message only)
            if (!isBatch && context.Message.Id != Guid.Empty)
            {
                context.Metadata.AddMessageAttribute(TraceContextKeys.MessageId, context.Message.Id.ToString());
                activity.SetTag("messaging.message.id", context.Message.Id.ToString());
            }

            try
            {
                var result = await func(stoppingToken).ConfigureAwait(false);
                activity.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.AddException(ex);
                throw;
            }
        }

        return await func(stoppingToken).ConfigureAwait(false);
    }
}
