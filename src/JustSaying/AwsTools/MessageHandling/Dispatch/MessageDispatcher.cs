using System.Diagnostics;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
#pragma warning disable CS0618
using JustSaying.Messaging.Middleware.Tracing;
#pragma warning restore CS0618

namespace JustSaying.AwsTools.MessageHandling.Dispatch;

internal sealed class MessageDispatcher : IMessageDispatcher
{
    private readonly IMessageMonitor _messagingMonitor;
    private readonly MiddlewareMap _middlewareMap;
    private readonly ILogger _logger;

#pragma warning disable CS0618
    private readonly TracingOptions _tracingOptions;
#pragma warning restore CS0618

#pragma warning disable CS0618
    public MessageDispatcher(
        IMessageMonitor messagingMonitor,
        MiddlewareMap middlewareMap,
        ILoggerFactory loggerFactory,
        TracingOptions tracingOptions = null)
#pragma warning restore CS0618
    {
        _messagingMonitor = messagingMonitor;
        _middlewareMap = middlewareMap;
        _logger = loggerFactory.CreateLogger("JustSaying");
        _tracingOptions = tracingOptions;
    }

    public async Task DispatchMessageAsync(
        IQueueMessageContext messageContext,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        (bool success, Message typedMessage, MessageAttributes attributes) =
            await DeserializeMessage(messageContext, cancellationToken).ConfigureAwait(false);

        if (!success)
        {
            _logger.LogTrace("DeserializeMessage failed. Message will not be dispatched.");
            return;
        }

        var messageType = typedMessage.GetType();
        var middleware = _middlewareMap.Get(messageContext.QueueName, messageType);

        if (middleware == null)
        {
            _logger.LogError(
                "Failed to dispatch. Middleware for message of type '{MessageTypeName}' not found in middleware map.",
                typedMessage.GetType().FullName);
            return;
        }

        var handleContext = new HandleMessageContext(
            messageContext.QueueName,
            messageContext.Message,
            typedMessage,
            messageType,
            messageContext,
            messageContext,
            messageContext.QueueUri,
            attributes);

        using var activity = StartConsumerActivity(messageContext, typedMessage, messageType, attributes);

        try
        {
            await middleware.RunAsync(handleContext, null, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.AddEvent(new ActivityEvent("exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", ex.GetType().FullName },
                        { "exception.message", ex.Message },
                        { "exception.stacktrace", ex.ToString() },
                    }));
            }

            throw;
        }
    }

    private Activity StartConsumerActivity(
        IQueueMessageContext messageContext,
        Message typedMessage,
        Type messageType,
        MessageAttributes attributes)
    {
        var traceParent = attributes?.Get(MessageAttributeKeys.TraceParent)?.StringValue;
        var traceState = attributes?.Get(MessageAttributeKeys.TraceState)?.StringValue;
        ActivityContext parsed = default;
        bool hasParsed = traceParent is not null
            && ActivityContext.TryParse(traceParent, traceState, out parsed);

        Activity activity;
#pragma warning disable CS0618
        if (hasParsed && _tracingOptions?.UseParentSpan == true)
#pragma warning restore CS0618
        {
            // Parent mode: consumer becomes child of producer (same trace ID)
            activity = JustSayingDiagnostics.ActivitySource.StartActivity(
                $"{messageContext.QueueName} process",
                ActivityKind.Consumer,
                parentContext: parsed);
        }
        else
        {
            // Link mode (default): new trace, linked back to producer
            var links = hasParsed
                ? [new ActivityLink(parsed)]
                : Array.Empty<ActivityLink>();
            activity = JustSayingDiagnostics.ActivitySource.StartActivity(
                $"{messageContext.QueueName} process",
                ActivityKind.Consumer,
                default(ActivityContext),
                tags: null,
                links: links);
        }

        if (activity is not null)
        {
            activity.SetTag("messaging.system", "aws_sqs");
            activity.SetTag("messaging.destination.name", messageContext.QueueName);
            activity.SetTag("messaging.operation.name", "process");
            activity.SetTag("messaging.operation.type", "process");
            activity.SetTag("messaging.message.id", messageContext.Message.MessageId);
            activity.SetTag("messaging.message.type", messageType.FullName);
        }

        return activity;
    }

    private async Task<(bool success, Message typedMessage, MessageAttributes attributes)>
        DeserializeMessage(IQueueMessageContext messageContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Attempting to deserialize message.");

            var (message, attributes) = await messageContext.MessageConverter.ConvertToInboundMessageAsync(messageContext.Message, cancellationToken);

            return (true, message, attributes);
        }
        catch (MessageFormatNotSupportedException ex)
        {
            _logger.LogWarning(ex,
                "Could not handle message with Id '{MessageId}' because a deserializer for the content is not configured. Message body: '{MessageBody}'.",
                messageContext.Message.MessageId,
                messageContext.Message.Body);

            await messageContext.DeleteMessage(cancellationToken).ConfigureAwait(false);
            _messagingMonitor.HandleError(ex, messageContext.Message);

            return (false, null, null);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
            return (false, null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deserializing message with Id '{MessageId}' and body '{MessageBody}'.",
                messageContext.Message.MessageId,
                messageContext.Message.Body);

            _messagingMonitor.HandleError(ex, messageContext.Message);

            return (false, null, null);
        }
    }
}
