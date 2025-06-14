using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling.Dispatch;

internal sealed class MessageDispatcher : IMessageDispatcher
{
    private readonly IMessageMonitor _messagingMonitor;
    private readonly MiddlewareMap _middlewareMap;

    private readonly ILogger _logger;

    public MessageDispatcher(
        IMessageMonitor messagingMonitor,
        MiddlewareMap middlewareMap,
        ILoggerFactory loggerFactory)
    {
        _messagingMonitor = messagingMonitor;
        _middlewareMap = middlewareMap;
        _logger = loggerFactory.CreateLogger("JustSaying");
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

        await middleware.RunAsync(handleContext, null, cancellationToken)
            .ConfigureAwait(false);
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
