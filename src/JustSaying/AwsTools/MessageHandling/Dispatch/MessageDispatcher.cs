using System.Text.Json;
using System.Text.Json.Nodes;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling.Dispatch;

public class MessageDispatcher : IMessageDispatcher
{
    private readonly IMessageSerializationRegister _serializationRegister;
    private readonly IMessageMonitor _messagingMonitor;
    private readonly MiddlewareMap _middlewareMap;
    private readonly MessageCompressionRegistry _compressionRegistry;
    // Temporary until we can remove the `IMessageSerializer` interface in favour of a cleaner design
    private readonly SystemTextJsonSerializer _jsonSerializer = new();

    private static ILogger _logger;

    public MessageDispatcher(
        IMessageSerializationRegister serializationRegister,
        IMessageMonitor messagingMonitor,
        MiddlewareMap middlewareMap,
        MessageCompressionRegistry compressionRegistry,
        ILoggerFactory loggerFactory)
    {
        _serializationRegister = serializationRegister;
        _messagingMonitor = messagingMonitor;
        _middlewareMap = middlewareMap;
        _compressionRegistry = compressionRegistry;
        _logger = loggerFactory.CreateLogger("JustSaying");
    }

    public MessageDispatcher(
        IMessageSerializationRegister serializationRegister,
        IMessageMonitor messagingMonitor,
        MiddlewareMap middlewareMap,
        ILoggerFactory loggerFactory) : this(
        serializationRegister,
        messagingMonitor,
        middlewareMap,
        null,
        loggerFactory)
    {
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
            _logger.LogDebug("Attempting to deserialize message with serialization register {Type}",
                _serializationRegister.GetType().FullName);

            var body = messageContext.Message.Body;

            var attributes = MessageAttributes(messageContext, ref body);

            var messageWithAttributes = _serializationRegister.DeserializeMessage(body);
            return (true, messageWithAttributes.Message, attributes);
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

    private MessageAttributes MessageAttributes(IQueueMessageContext messageContext, ref string body)
    {
        bool isRawMessage = IsRawMessage(body);
        var attributes = isRawMessage ? GetRawMessageAttributes(messageContext) : _jsonSerializer.GetMessageAttributes(body);

        var contentEncoding = attributes.Get(MessageAttributeKeys.ContentEncoding);
        if (contentEncoding is not null)
        {
            var decompressor = _compressionRegistry.GetCompression(contentEncoding.StringValue);
            if (decompressor is null)
            {
                throw new InvalidOperationException($"Compression encoding '{contentEncoding.StringValue}' is not registered.");
            }

            var jsonNode = JsonNode.Parse(body)!;
            var messageNode = jsonNode["Message"]!;
            string json = messageNode.ToString();

            var decompressedBody = decompressor.Decompress(json);

            jsonNode["Message"] = JsonValue.Create(decompressedBody);
            body = jsonNode.ToJsonString();
        }

        return attributes;
    }

    private static bool IsRawMessage(string body)
    {
        bool isRawMessage = true;
        using var jsonDocument = JsonDocument.Parse(body);
        if (jsonDocument.RootElement.TryGetProperty("Type", out var typeElement))
        {
            var messageType = typeElement.GetString();
            if (messageType is "Notification")
            {
                isRawMessage = false;
            }
        }

        return isRawMessage;
    }

    private static MessageAttributes GetRawMessageAttributes(IQueueMessageContext messageContext)
    {
        Dictionary<string, MessageAttributeValue> rawAttributes = new ();
        if (messageContext.Message.MessageAttributes is null)
        {
            return new MessageAttributes();
        }

        foreach (var messageMessageAttribute in messageContext.Message.MessageAttributes)
        {
            var dataType = messageMessageAttribute.Value.DataType;
            var dataValue = messageMessageAttribute.Value.StringValue;
            var isString = dataType == "String";
            var messageAttributeValue = new MessageAttributeValue
            {
                DataType = dataType,
                StringValue = isString ? dataValue : null,
                BinaryValue = !isString ? Convert.FromBase64String(dataValue) : null
            };
            rawAttributes.Add(messageMessageAttribute.Key, messageAttributeValue);
        }

        return new MessageAttributes(rawAttributes);
    }
}
