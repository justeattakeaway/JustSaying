using System.Diagnostics;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Middleware.Tracing;

/// <summary>
/// Middleware that creates an Activity for message handling with trace context propagation.
/// Supports two modes configured via <see cref="TracingOptions"/>:
/// <list type="bullet">
///   <item><b>Link mode</b> (default): Consumer creates a new trace root with a link to the producer span.</item>
///   <item><b>Parent mode</b>: Consumer span becomes a child of the producer span, forming a single trace.</item>
/// </list>
/// </summary>
public class TracingMiddleware(TracingOptions options) : MiddlewareBase<HandleMessageContext, bool>
{
    private static readonly ActivitySource ActivitySource = new("JustSaying.MessageHandler");

    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        var messageType = context.Message.GetType().Name;
        var activityName = $"process {messageType}";

        var parentContext = ExtractParentContext(context.MessageAttributes);

        Activity activity;
        if (options.UseParentSpan && parentContext != default)
        {
            // Parent mode: consumer span is a child of the producer span
            activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext);
        }
        else
        {
            // Link mode (default): consumer is a new trace root, linked to the producer
            var links = parentContext != default
                ? new[] { new ActivityLink(parentContext) }
                : Array.Empty<ActivityLink>();
            activity = ActivitySource.StartActivity(
                activityName, ActivityKind.Consumer, parentContext: default, links: links);
        }

        using (activity)
        {
            if (activity != null)
            {
                activity.SetTag("messaging.system", "aws_sqs");
                activity.SetTag("messaging.operation", "process");
                activity.SetTag("messaging.destination.name", context.QueueName);
                activity.SetTag("messaging.message.type", messageType);

                if (context.MessageAttributes.Get(TraceContextKeys.MessageId) is { } messageId)
                {
                    activity.SetTag("messaging.message.id", messageId.StringValue);
                }

                try
                {
                    var result = await func(stoppingToken).ConfigureAwait(false);
                    activity.SetStatus(result ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
                    return result;
                }
                catch (Exception ex)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                    {
                        { "exception.type", ex.GetType().FullName },
                        { "exception.message", ex.Message },
                        { "exception.stacktrace", ex.ToString() }
                    }));
                    throw;
                }
            }

            return await func(stoppingToken).ConfigureAwait(false);
        }
    }

    private static ActivityContext ExtractParentContext(MessageAttributes attributes)
    {
        DistributedContextPropagator.Current.ExtractTraceIdAndState(attributes,
            static (object carrier, string fieldName, out string fieldValue, out IEnumerable<string> fieldValues) =>
            {
                fieldValue = ((MessageAttributes)carrier).Get(fieldName)?.StringValue;
                fieldValues = null;
            },
            out var traceparent,
            out var tracestate);

        if (traceparent != null && ActivityContext.TryParse(traceparent, tracestate, out var parentContext))
        {
            return parentContext;
        }

        return default;
    }
}
