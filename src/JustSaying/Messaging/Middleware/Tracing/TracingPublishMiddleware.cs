using System.Diagnostics;

namespace JustSaying.Messaging.Middleware.Tracing;

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

            // Propagate trace context via message attributes using the platform propagator
            DistributedContextPropagator.Current.Inject(activity, context.Metadata,
                static (carrier, key, value) =>
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        ((PublishMetadata)carrier).AddMessageAttribute(key, value);
                    }
                });

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
