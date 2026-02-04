using System.Diagnostics;
using JustSaying.Messaging;
using JustSaying.Models;
using OpenTelemetry.Trace;

namespace JustSaying.Sample.ServiceDefaults.Tracing;

/// <summary>
/// Extension methods for publishing messages with distributed trace context.
/// </summary>
public static class TracingPublishExtensions
{
    private static readonly ActivitySource ActivitySource = new("JustSaying.MessagePublisher");

    /// <summary>
    /// Publishes a message with distributed trace context attached.
    /// Creates a producer span and propagates trace context via message attributes.
    /// </summary>
    public static async Task PublishWithTracingAsync<T>(
        this IMessagePublisher publisher,
        T message,
        CancellationToken cancellationToken = default) where T : Message
    {
        var messageType = typeof(T).Name;
        using var activity = ActivitySource.StartActivity(
            $"publish {messageType}",
            ActivityKind.Producer);

        var metadata = new PublishMetadata();

        if (activity != null)
        {
            activity.SetTag("messaging.system", "aws_sns");
            activity.SetTag("messaging.operation", "publish");
            activity.SetTag("messaging.message.type", messageType);

            // Add W3C trace context to message attributes
            var traceparent = $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}";
            metadata.AddMessageAttribute(TraceContextKeys.TraceParent, traceparent);

            if (activity.TraceStateString != null)
            {
                metadata.AddMessageAttribute(TraceContextKeys.TraceState, activity.TraceStateString);
            }

            // Add message ID for correlation
            if (message.Id != Guid.Empty)
            {
                metadata.AddMessageAttribute(TraceContextKeys.MessageId, message.Id.ToString());
                activity.SetTag("messaging.message.id", message.Id.ToString());
            }
        }

        try
        {
            await publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple messages with distributed trace context attached.
    /// Creates a producer span for the batch and propagates trace context via message attributes.
    /// </summary>
    public static async Task PublishWithTracingAsync<T>(
        this IMessageBatchPublisher publisher,
        IEnumerable<T> messages,
        CancellationToken cancellationToken = default) where T : Message
    {
        var messageType = typeof(T).Name;
        var messageList = messages.ToList();

        using var activity = ActivitySource.StartActivity(
            $"publish batch {messageType}",
            ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetTag("messaging.system", "aws_sns");
            activity.SetTag("messaging.operation", "publish");
            activity.SetTag("messaging.message.type", messageType);
            activity.SetTag("messaging.batch.message_count", messageList.Count);

            // Add W3C trace context to message attributes
            var traceparent = $"00-{activity.TraceId}-{activity.SpanId}-{(activity.Recorded ? "01" : "00")}";
            var metadata = new PublishBatchMetadata();
            metadata.AddMessageAttribute(TraceContextKeys.TraceParent, traceparent);

            if (activity.TraceStateString != null)
            {
                metadata.AddMessageAttribute(TraceContextKeys.TraceState, activity.TraceStateString);
            }

            try
            {
                await publisher.PublishAsync(messageList, metadata, cancellationToken).ConfigureAwait(false);
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.AddException(ex);
                throw;
            }
        }
        else
        {
            await publisher.PublishAsync(messageList, cancellationToken).ConfigureAwait(false);
        }
    }
}
