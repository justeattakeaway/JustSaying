using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling.Dispatch;

public class MessageDispatcher : IMessageDispatcher
{
    private readonly IMessageSerializationRegister _serializationRegister;
    private readonly IMessageMonitor _messagingMonitor;
    private readonly MiddlewareMap _middlewareMap;

    private static ILogger _logger;

    public MessageDispatcher(
        IMessageSerializationRegister serializationRegister,
        IMessageMonitor messagingMonitor,
        MiddlewareMap middlewareMap,
        ILoggerFactory loggerFactory)
    {
        _serializationRegister = serializationRegister;
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

        (bool success, object messageInstance, MessageAttributes attributes) =
            await DeserializeMessage(messageContext, cancellationToken).ConfigureAwait(false);

        if (!success)
        {
            return;
        }

        var messageType = messageInstance.GetType();
        var middleware = _middlewareMap.Get(messageContext.QueueName, messageType);

        if (middleware == null)
        {
            _logger.LogError(
                "Failed to dispatch. Middleware for message of type '{MessageTypeName}' not found in middleware map.",
                messageInstance.GetType().FullName);
            return;
        }

        var handleContext = new HandleMessageContext(
            messageContext.QueueName,
            messageContext.Message,
            messageInstance,
            messageType,
            messageContext,
            messageContext,
            messageContext.QueueUri,
            attributes);

        await middleware.RunAsync(handleContext, null, cancellationToken)
            .ConfigureAwait(false);

    }

    private async Task<(bool success, object messageInstance, MessageAttributes attributes)> DeserializeMessage(IQueueMessageContext messageContext, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Attempting to deserialize message with serialization register {Type}",
                _serializationRegister.GetType().FullName);
            var messageWithAttributes = _serializationRegister.DeserializeMessage(messageContext.Message.Body);
            return (true, messageWithAttributes.Message, messageWithAttributes.MessageAttributes);
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
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
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
