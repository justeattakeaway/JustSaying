using System.Text.Json;
using System.Text.Json.Serialization;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// A class representing an implementation of <see cref="IMessageSerializer"/> for the <c>System.Text.Json</c> serializer.
/// </summary>
public class SystemTextJsonSerializer : IMessageSerializer, IMessageAndAttributesDeserializer
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
#if NET8_0_OR_GREATER
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#else
        IgnoreNullValues = true,
#endif
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/> class.
    /// </summary>
    public SystemTextJsonSerializer()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/> class.
    /// </summary>
    /// <param name="options">The optional <see cref="JsonSerializerOptions"/> to use.</param>
    public SystemTextJsonSerializer(JsonSerializerOptions options)
    {
        _options = options ?? DefaultJsonSerializerOptions;
    }

    /// <inheritdoc />
    public string GetMessageSubject(string sqsMessage)
    {
        using var body = JsonDocument.Parse(sqsMessage);
        string subject = string.Empty;

        if (body.RootElement.TryGetProperty("Subject", out var value))
        {
            subject = value.GetString() ?? string.Empty;
        }

        return subject;
    }

    public MessageAttributes GetMessageAttributes(string message)
    {
        using var jsonDocument = JsonDocument.Parse(message);
        return GetMessageAttributes(jsonDocument);
    }

    /// <inheritdoc />
    public Message Deserialize(string message, Type type)
    {
        using var document = JsonDocument.Parse(message);
        return Deserialize(document, type);
    }

    /// <inheritdoc />
    public string Serialize(Message message, bool serializeForSnsPublishing, string subject)
    {
        string json = JsonSerializer.Serialize(message, message.GetType(), _options);

        // AWS SNS service will add Subject and Message properties automatically,
        // so just return plain message
        if (serializeForSnsPublishing)
        {
            return json;
        }

        // For direct publishing to SQS, add Subject and Message properties manually
        var context = new { Subject = subject, Message = json };
        return JsonSerializer.Serialize(context, _options);
    }

    MessageWithAttributes IMessageAndAttributesDeserializer.DeserializeWithAttributes(string message, Type type)
    {
        using var document = JsonDocument.Parse(message);

        var content = Deserialize(document, type);
        var attributes = GetMessageAttributes(document);

        return new(content, attributes);
    }

    private Message Deserialize(JsonDocument document, Type type)
    {
        JsonElement element = document.RootElement.GetProperty("Message");
        string json = element.ToString();

        return (Message)JsonSerializer.Deserialize(json, type, _options);
    }

    private MessageAttributes GetMessageAttributes(JsonDocument jsonDocument)
    {
        if (!jsonDocument.RootElement.TryGetProperty("MessageAttributes", out var attributesElement))
        {
            return new MessageAttributes();
        }

        var attributes = new Dictionary<string, MessageAttributeValue>();
        foreach (var property in attributesElement.EnumerateObject())
        {
            var dataType = property.Value.GetProperty("Type").GetString();
            var dataValue = property.Value.GetProperty("Value").GetString();

            attributes.Add(property.Name, MessageAttributeParser.Parse(dataType, dataValue));
        }

        return new MessageAttributes(attributes);
    }
}
