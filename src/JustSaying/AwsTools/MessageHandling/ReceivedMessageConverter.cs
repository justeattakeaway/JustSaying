using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JustSaying.AwsTools;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

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

    public ValueTask<ReceivedMessage> ConvertForReceiveAsync(Amazon.SQS.Model.Message message)
    {
        string body = message.Body;
        var attributes = GetMessageAttributes(message, body);

        if (body is not null && !_isRawMessage)
        {
            var jsonNode = JsonNode.Parse(body);
            if (jsonNode is JsonObject jsonObject && jsonObject.TryGetPropertyValue("Message", out var messageNode))
            {
                body = messageNode?.GetValue<string>();
            }
        }
        body = ApplyBodyDecompression(body, attributes);
        var result = _bodySerializer.Deserialize(body);
        return new ValueTask<ReceivedMessage>(new ReceivedMessage(result, attributes));
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
        return IsSnsPayload(body) ? GetMessageAttributes(body) : GetRawMessageAttributes(message);
    }

    private static MessageAttributes GetMessageAttributes(string message)
    {
        using var jsonDocument = JsonDocument.Parse(message);

        if (!jsonDocument.RootElement.TryGetProperty("MessageAttributes", out var attributesElement))
        {
            return new MessageAttributes();
        }

        Dictionary<string, MessageAttributeValue> attributes = new();
        foreach (var property in attributesElement.EnumerateObject())
        {
            var dataType = property.Value.GetProperty("Type").GetString();
            var dataValue = property.Value.GetProperty("Value").GetString();

            attributes.Add(property.Name, MessageAttributeParser.Parse(dataType, dataValue));
        }

        return new MessageAttributes(attributes);
    }

    private static MessageAttributes GetRawMessageAttributes(Amazon.SQS.Model.Message message)
    {
        if (message.MessageAttributes is null)
        {
            return new MessageAttributes();
        }

        Dictionary<string, MessageAttributeValue> rawAttributes = new ();

        foreach (var messageMessageAttribute in message.MessageAttributes)
        {
            var dataType = messageMessageAttribute.Value.DataType;
            var dataValue = messageMessageAttribute.Value.StringValue;
            rawAttributes.Add(messageMessageAttribute.Key, MessageAttributeParser.Parse(dataType, dataValue));
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
            var utf8JsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(body));
            if (!JsonDocument.TryParseValue(ref utf8JsonReader, out var jsonDocument))
            {
                return false;
            }

            using (jsonDocument)
            {
                if (jsonDocument.RootElement.TryGetProperty("Type", out var typeElement))
                {
                    return typeElement.GetString() is "Notification";
                }
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }
}
