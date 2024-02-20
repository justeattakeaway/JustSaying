using System.Text.Json;
using System.Text.Json.Serialization;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// A class representing an implementation of <see cref="IMessageSerializer"/> for the <c>System.Text.Json</c> serializer.
/// </summary>
#if NET8_0_OR_GREATER
[RequiresUnreferencedCode(Constants.SerializationUnreferencedCodeMessage)]
[RequiresDynamicCode(Constants.SerializationDynamicCodeMessage)]
#endif
public partial class SystemTextJsonSerializer : IMessageSerializer
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
        var jsonDocument = JsonDocument.Parse(message);

        if (!jsonDocument.RootElement.TryGetProperty("MessageAttributes", out var attributesElement))
        {
            return new MessageAttributes();
        }

        var attributes = new Dictionary<string, MessageAttributeValue>();
        foreach(var obj in attributesElement.EnumerateObject())
        {
            var dataType = obj.Value.GetProperty("Type").GetString();
            var dataValue = obj.Value.GetProperty("Value").GetString();

            var isString = dataType == "String";

            attributes.Add(obj.Name, new MessageAttributeValue()
            {
                DataType = dataType,
                StringValue = isString ? dataValue : null,
                BinaryValue = !isString ? Convert.FromBase64String(dataValue) : null
            });
        }

        return new MessageAttributes(attributes);
    }

    /// <inheritdoc />
    public Message Deserialize(string message, Type type)
    {
        using var document = JsonDocument.Parse(message);
        JsonElement element = document.RootElement.GetProperty("Message");
        string json = element.ToString();

        return (Message)JsonSerializer.Deserialize(json, type, _options);
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
}
