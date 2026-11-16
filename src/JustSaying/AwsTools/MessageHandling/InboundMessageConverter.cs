using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JustSaying.AwsTools;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.Messaging;

internal sealed class InboundMessageConverter : IInboundMessageConverter
{
    private readonly IInboundMessageSerializerResolver _serializerResolver;
    private readonly MessageCompressionRegistry _compressionRegistry;
    private readonly bool _isRawMessage;

    public InboundMessageConverter(IMessageBodySerializer bodySerializer, MessageCompressionRegistry compressionRegistry, bool isRawMessage)
        : this(new SingleInboundMessageSerializerResolver(bodySerializer), compressionRegistry, isRawMessage)
    {
    }

    public InboundMessageConverter(IInboundMessageSerializerResolver serializerResolver, MessageCompressionRegistry compressionRegistry, bool isRawMessage)
    {
        _serializerResolver = serializerResolver ?? throw new ArgumentNullException(nameof(serializerResolver));
        _compressionRegistry = compressionRegistry ?? throw new ArgumentNullException(nameof(compressionRegistry));
        _isRawMessage = isRawMessage;
    }

    public ValueTask<InboundMessage> ConvertToInboundMessageAsync(Amazon.SQS.Model.Message message, CancellationToken cancellationToken = default)
    {
        string body = message.Body;
        var attributes = GetMessageAttributes(message, body);

        string subject = null;
        JsonObject parsedBody = null;
        if (body is not null && !_isRawMessage && TryParseEnvelope(body, out var jsonObject))
        {
            if (jsonObject.TryGetPropertyValue("Message", out var messageNode))
            {
                body = messageNode?.GetValue<string>();
            }
            else
            {
                // No envelope, so the parsed object is the message body itself.
                parsedBody = jsonObject;
            }

            if (jsonObject.TryGetPropertyValue("Subject", out var subjectNode))
            {
                subject = subjectNode?.GetValue<string>();
            }
        }

        var decompressedBody = ApplyBodyDecompression(body, attributes);
        if (!ReferenceEquals(decompressedBody, body))
        {
            parsedBody = null;
        }

        body = decompressedBody;
        var serializer = _serializerResolver.Resolve(body, subject, attributes);

        // A serializer that doesn't know about CloudEvents would read the envelope's attributes as the
        // message's properties and silently produce an all-default message, so fail it instead.
        if (serializer is not ISelfDescribingMessageBodySerializer && TryGetCloudEventType(body, parsedBody, out var cloudEventType))
        {
            throw new UnroutableMessageException(
                $"The message is a CloudEvent (type '{cloudEventType}'), but the subscription reads it with a serializer that doesn't understand " +
                "CloudEvents, so it would be read as a message with default values. To consume CloudEvents, subscribe with the JustSaying.CloudEvents " +
                "package: ForCloudEventTopicData<T>(type) or ForCloudEventTopic<T>(type) for a topic, HandlingCloudEventData<T>(type) or " +
                "HandlingCloudEvent<T>(type) on a ForQueue(...) subscription, or pass a CloudEvents serializer to WithMessageBodySerializer(...). " +
                $"A custom serializer that reads CloudEvents should implement {nameof(ISelfDescribingMessageBodySerializer)}.");
        }

        var result = serializer.Deserialize(body);
        return new ValueTask<InboundMessage>(new InboundMessage(result, attributes));
    }

    /// <summary>
    /// Determines whether a body is a structured-mode CloudEvent: a JSON object with the required
    /// <c>specversion</c>, <c>id</c>, <c>source</c> and <c>type</c> string attributes.
    /// </summary>
    /// <param name="body">The unwrapped, decompressed message body.</param>
    /// <param name="parsedBody"><paramref name="body"/> already parsed, if it was; otherwise <see langword="null"/>.</param>
    /// <param name="type">When this method returns <see langword="true"/>, the CloudEvents <c>type</c>.</param>
    private static bool TryGetCloudEventType(string body, JsonObject parsedBody, out string type)
    {
        type = null;

        // Only parse a body again when it could be a CloudEvent, so ordinary messages don't pay for it.
        if (parsedBody is null
            && (body is null || body.IndexOf("\"specversion\"", StringComparison.Ordinal) < 0 || !TryParseEnvelope(body, out parsedBody)))
        {
            return false;
        }

        static bool IsString(JsonObject jsonObject, string name, out string value)
        {
            value = null;
            return jsonObject.TryGetPropertyValue(name, out var node)
                && node is JsonValue jsonValue
                && jsonValue.TryGetValue(out value);
        }

        return IsString(parsedBody, "specversion", out _)
            && IsString(parsedBody, "id", out _)
            && IsString(parsedBody, "source", out _)
            && IsString(parsedBody, "type", out type);
    }

    /// <summary>
    /// Attempts to read the <c>{Message, Subject}</c> queue envelope from a body. A body that is not a
    /// JSON object simply has no envelope to strip and is passed through untouched — that covers a
    /// publication whose serializer is self-describing (CloudEvents), which is sent without the envelope,
    /// and a compressed body, which arrives as a bare Base64 string rather than JSON.
    /// </summary>
    private static bool TryParseEnvelope(string body, out JsonObject envelope)
    {
        envelope = null;

        var trimmed = body.AsSpan().TrimStart();
        if (trimmed.IsEmpty || trimmed[0] != '{')
        {
            return false;
        }

        try
        {
            envelope = JsonNode.Parse(body) as JsonObject;
        }
        catch (JsonException)
        {
            return false;
        }

        return envelope is not null;
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

            attributes.Add(property.Name, ParseMessageAttribute(dataType, dataValue));
        }

        return new MessageAttributes(attributes);
    }

    private static MessageAttributeValue ParseMessageAttribute(string dataType, string dataValue)
    {
        // Check for a prefix instead of an exact match as SQS supports custom-type labels, or example, "Binary.gif".
        // See https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-message-metadata.html#sqs-message-attributes.
        bool isBinary = dataType?.StartsWith("Binary", StringComparison.Ordinal) is true;

        return new()
        {
            DataType = dataType,
            StringValue = !isBinary ? dataValue : null,
            BinaryValue = isBinary ? Convert.FromBase64String(dataValue) : null
        };
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
            rawAttributes.Add(messageMessageAttribute.Key, ParseMessageAttribute(dataType, dataValue));
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
