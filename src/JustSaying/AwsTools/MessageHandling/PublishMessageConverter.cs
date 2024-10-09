using System.Text;
using System.Text.Json.Nodes;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.Messaging;

internal sealed class PublishMessageConverter : IPublishMessageConverter
{
    private readonly PublishDestinationType _destinationType;
    private readonly IMessageBodySerializer _bodySerializer;
    private readonly MessageCompressionRegistry _compressionRegistry;
    private readonly PublishCompressionOptions _compressionOptions;
    private readonly string _subject;
    private readonly bool _isRawMessage;

    public PublishMessageConverter(PublishDestinationType destinationType, IMessageBodySerializer bodySerializer, MessageCompressionRegistry compressionRegistry, PublishCompressionOptions compressionOptions, string subject, bool isRawMessage)
    {
        _destinationType = destinationType;
        _bodySerializer = bodySerializer;
        _compressionRegistry = compressionRegistry;
        _compressionOptions = compressionOptions;
        _subject = subject;
        _isRawMessage = isRawMessage;
    }

    public ValueTask<PublishMessage> ConvertForPublishAsync(Message message, PublishMetadata publishMetadata)
    {
        var messageBody = _bodySerializer.Serialize(message);

        Dictionary<string, MessageAttributeValue> attributeValues = new();
        AddMessageAttributes(attributeValues, publishMetadata);

        (string compressedMessage, string contentEncoding) = CompressMessageBody(messageBody, publishMetadata);
        if (compressedMessage is not null)
        {
            messageBody = compressedMessage;
            attributeValues.Add(MessageAttributeKeys.ContentEncoding, new MessageAttributeValue { DataType = "String", StringValue = contentEncoding });
        }

        if (_destinationType == PublishDestinationType.Queue && !_isRawMessage)
        {
            messageBody = new JsonObject
            {
                ["Message"] = messageBody,
                ["Subject"] = _subject
            }.ToJsonString();
        }

        return new ValueTask<PublishMessage>(new PublishMessage(messageBody, attributeValues, _subject));
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
    /// <returns>A tuple containing the compressed message (or null if not compressed) and the content encoding used (or null if not compressed).</returns>
    internal (string compressedMessage, string contentEncoding) CompressMessageBody(string message, PublishMetadata metadata)
    {
        string contentEncoding = null;
        string compressedMessage = null;
        if (_compressionOptions?.CompressionEncoding is { } compressionEncoding && _compressionRegistry is not null)
        {
            var messageSize = CalculateTotalMessageSize(message, metadata);
            if (messageSize >= _compressionOptions.MessageLengthThreshold)
            {
                var compression = _compressionRegistry.GetCompression(compressionEncoding);
                if (compression is null)
                {
                    throw new InvalidOperationException($"No compression algorithm registered for encoding '{compressionEncoding}'.");
                }

                JsonNode jsonNode = null;
                if (_destinationType == PublishDestinationType.Queue && !_isRawMessage)
                {
                    jsonNode = JsonNode.Parse(message);
                    if (jsonNode is JsonObject jsonObject && jsonObject.TryGetPropertyValue("Message", out var messageNode))
                    {
                        message = messageNode?.GetValue<string>();
                    }
                }
                compressedMessage = compression.Compress(message);
                contentEncoding = compressionEncoding;
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
