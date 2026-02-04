using System.Diagnostics;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using OpenTelemetry.Trace;

namespace JustSaying.Sample.ServiceDefaults.Tracing;

/// <summary>
/// Middleware that creates an Activity for message handling with a link to the original publish span.
/// This creates a proper distributed trace where the handler span links back to the publisher,
/// rather than being a child of the SQS.ReceiveMessage span.
/// </summary>
public class TracingMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    private static readonly ActivitySource ActivitySource = new("JustSaying.MessageHandler");

    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        var messageType = context.Message.GetType().Name;
        var activityName = $"process {messageType}";

        // Try to extract trace context from message attributes
        var links = ExtractLinks(context.MessageAttributes);

        // Create a new activity with links to the original trace (not as parent)
        using var activity = ActivitySource.StartActivity(
            activityName,
            ActivityKind.Consumer,
            parentContext: default, // No parent - this is a new trace root
            links: links);

        if (activity != null)
        {
            activity.SetTag("messaging.system", "aws_sqs");
            activity.SetTag("messaging.operation", "process");
            activity.SetTag("messaging.destination.name", context.QueueName);
            activity.SetTag("messaging.message.type", messageType);

            // Add message ID if available
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
                activity.AddException(ex);
                throw;
            }
        }

        return await func(stoppingToken).ConfigureAwait(false);
    }

    private static IEnumerable<ActivityLink> ExtractLinks(MessageAttributes attributes)
    {
        var traceparent = attributes.Get(TraceContextKeys.TraceParent);
        if (traceparent?.StringValue == null)
        {
            yield break;
        }

        // Parse W3C traceparent format: version-traceid-spanid-flags
        // Example: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
        if (ActivityContext.TryParse(traceparent.StringValue, null, out var parentContext))
        {
            yield return new ActivityLink(parentContext);
        }
    }
}
