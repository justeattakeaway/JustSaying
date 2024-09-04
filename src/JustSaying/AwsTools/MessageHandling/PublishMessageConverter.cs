using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.Messaging;

internal sealed class ReceivedMessageConverter : IReceivedMessageConverter
{
    private readonly IMessageBodySerializer _bodySerializer;
    private readonly MessageCompressionRegistry _compressionRegistry;
    private readonly bool _isRawMessage;

    public ReceivedMessageConverter(IMessageBodySerializer bodySerializer, MessageCompressionRegistry compressionRegistry, bool isRawMessage)
    {
        _bodySerializer = bodySerializer;
        _compressionRegistry = compressionRegistry;
        _isRawMessage = isRawMessage;
    }

    public ReceivedMessage ConvertForReceive(Amazon.SQS.Model.Message message)
    {
        string body = message.Body;
        var attributes = GetMessageAttributes(message, body);

        if (body is not null && !_isRawMessage)
        {
            var jsonNode = JsonNode.Parse(body);
            if (jsonNode is JsonObject jsonObject && jsonObject.TryGetPropertyValue("Message", out var messageNode))
            {
                body = messageNode!.ToString();
            }
        }
        body = ApplyBodyDecompression(body, attributes);
        var result = _bodySerializer.Deserialize(body);
        return new ReceivedMessage(result, attributes);
    }

    private string ApplyBodyDecompression(string body, MessageAttributes attributes)
    {
        var contentEncoding = attributes.Get(MessageAttributeKeys.ContentEncoding);
        if (contentEncoding is not null)
        {
            var decompressor = _compressionRegistry.GetCompression(contentEncoding.StringValue);
            if (decompressor is null)
            {
                throw new InvalidOperationException($"Compression encoding '{contentEncoding.StringValue}' is not registered.");
            }

            body = decompressor.Decompress(body);
        }

        return body;
    }

    private static MessageAttributes GetMessageAttributes(Amazon.SQS.Model.Message message, string body)
    {
        bool isSnsPayload = IsSnsPayload(body);
        var attributes = isSnsPayload ? GetMessageAttributes(body) : GetRawMessageAttributes(message);

        return attributes;
    }

    private static MessageAttributes GetMessageAttributes(string message)
    {
        using var jsonDocument = JsonDocument.Parse(message);

        if (!jsonDocument.RootElement.TryGetProperty("MessageAttributes", out var attributesElement))
        {
            return new MessageAttributes();
        }

        Dictionary<string, MessageAttributeValue> attributes = new();
        foreach(var obj in attributesElement.EnumerateObject())
        {
            var dataType = obj.Value.GetProperty("Type").GetString();
            var dataValue = obj.Value.GetProperty("Value").GetString();

            var isString = dataType == "String";

            attributes.Add(obj.Name, new MessageAttributeValue
            {
                DataType = dataType,
                StringValue = isString ? dataValue : null,
                BinaryValue = !isString ? Convert.FromBase64String(dataValue) : null
            });
        }

        return new MessageAttributes(attributes);
    }

    private static MessageAttributes GetRawMessageAttributes(Amazon.SQS.Model.Message message)
    {
        Dictionary<string, MessageAttributeValue> rawAttributes = new ();
        if (message.MessageAttributes is null)
        {
            return new MessageAttributes();
        }

        foreach (var messageMessageAttribute in message.MessageAttributes)
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

    private static bool IsSnsPayload(string body)
    {
        if (body is null)
        {
            return false;
        }

        try
        {
            var isSnsPayload = false;
            using var jsonDocument = JsonDocument.Parse(body);
            if (jsonDocument.RootElement.TryGetProperty("Type", out var typeElement))
            {
                string messageType = typeElement.GetString();
                if (messageType is "Notification")
                {
                    isSnsPayload = true;
                }
            }

            return isSnsPayload;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class PublishMessageConverter : IPublishMessageConverter
{
    private readonly IMessageBodySerializer _bodySerializer;
    private readonly MessageCompressionRegistry _compressionRegistry;
    private readonly PublishCompressionOptions _compressionOptions;
    private readonly string _snsSubject;

    public PublishMessageConverter(IMessageBodySerializer bodySerializer, MessageCompressionRegistry compressionRegistry, PublishCompressionOptions compressionOptions, string snsSubject)
    {
        _bodySerializer = bodySerializer;
        _compressionRegistry = compressionRegistry;
        _compressionOptions = compressionOptions;
        _snsSubject = snsSubject;
    }

    public PublishMessage ConvertForPublish(Message message, PublishMetadata publishMetadata, PublishDestinationType destinationType)
    {
        var messageBody = _bodySerializer.Serialize(message);

        Dictionary<string, MessageAttributeValue> attributeValues = new();
        AddMessageAttributes(attributeValues, publishMetadata);

        (string compressedMessage, string contentEncoding) = CompressMessageBody(messageBody, publishMetadata, destinationType, _compressionOptions, _compressionRegistry);
        if (compressedMessage is not null)
        {
            messageBody = compressedMessage;
            attributeValues.Add(MessageAttributeKeys.ContentEncoding, new MessageAttributeValue { DataType = "String", StringValue = contentEncoding });
        }

        return new PublishMessage(messageBody, attributeValues, _snsSubject);
    }

    private static void AddMessageAttributes(Dictionary<string, MessageAttributeValue> requestMessageAttributes, PublishMetadata metadata)
    {
        if (metadata?.MessageAttributes == null || metadata.MessageAttributes.Count == 0)
        {
            return;
        }

        foreach (var attribute in metadata.MessageAttributes)
        {
            requestMessageAttributes.Add(attribute.Key, attribute.Value);
        }
    }

    /// <summary>
    /// Compresses a message if it meets the specified compression criteria.
    /// </summary>
    /// <param name="message">The original message to potentially compress.</param>
    /// <param name="metadata">Metadata associated with the message.</param>
    /// <param name="destinationType">The type of destination (<see cref="PublishDestinationType.Topic"/> or <see cref="PublishDestinationType.Queue"/>) for the message.</param>
    /// <param name="compressionOptions">Options specifying when and how to compress.</param>
    /// <param name="compressionRegistry">Registry of available compression algorithms.</param>
    /// <returns>A tuple containing the compressed message (or null if not compressed) and the content encoding used (or null if not compressed).</returns>
    private static (string compressedMessage, string contentEncoding) CompressMessageBody(string message, PublishMetadata metadata, PublishDestinationType destinationType, PublishCompressionOptions compressionOptions, MessageCompressionRegistry compressionRegistry)
    {
        string contentEncoding = null;
        string compressedMessage = null;
        if (compressionOptions?.CompressionEncoding is { } compressionEncoding && compressionRegistry is not null)
        {
            var messageSize = CalculateTotalMessageSize(message, metadata);
            if (messageSize > compressionOptions.MessageLengthThreshold)
            {
                var compression = compressionRegistry.GetCompression(compressionEncoding);
                if (compression is null)
                {
                    throw new InvalidOperationException($"No compression algorithm registered for encoding '{compressionEncoding}'.");
                }

                JsonNode jsonNode = null;
                if (destinationType == PublishDestinationType.Queue)
                {
                    jsonNode = JsonNode.Parse(message);
                    if (jsonNode is JsonObject jsonObject && jsonObject.TryGetPropertyValue("Message", out var messageNode))
                    {
                        message = messageNode!.GetValue<string>();
                    }
                }
                compressedMessage = compression.Compress(message);
                contentEncoding = compressionEncoding;

                if (destinationType == PublishDestinationType.Queue)
                {
                    if (jsonNode is JsonObject jsonObject)
                    {
                        jsonObject["Message"] = compressedMessage;
                        compressedMessage = jsonObject.ToJsonString();
                    }
                }
            }
        }

        return (compressedMessage, contentEncoding);
    }

    /// <summary>
    /// Calculates the total size of a message, including its metadata.
    /// </summary>
    /// <param name="message">The message content.</param>
    /// <param name="metadata">Metadata associated with the message.</param>
    /// <returns>The total size of the message in bytes.</returns>
    private static int CalculateTotalMessageSize(string message, PublishMetadata metadata)
    {
        var messageSize = Encoding.UTF8.GetByteCount(message);
        if (metadata?.MessageAttributes != null)
        {
            foreach (var attribute in metadata.MessageAttributes)
            {
                messageSize += Encoding.UTF8.GetByteCount(attribute.Key);
                messageSize += Encoding.UTF8.GetByteCount(attribute.Value.DataType);
                if (attribute.Value.StringValue is not null)
                {
                    messageSize += Encoding.UTF8.GetByteCount(attribute.Value.StringValue);
                }

                if (attribute.Value.BinaryValue is not null)
                {
                    messageSize += attribute.Value.BinaryValue.Count;
                }
            }
        }

        return messageSize;
    }
}
