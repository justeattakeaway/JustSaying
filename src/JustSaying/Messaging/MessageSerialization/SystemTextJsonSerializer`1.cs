using System.Text.Json;
using System.Text.Json.Serialization;
using JustSaying.Extensions;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// A class representing an implementation of <see cref="IMessageSerializer"/> for the <c>System.Text.Json</c> serializer.
/// </summary>
public class SystemTextJsonSerializer<T> : IMessageSerializer
    where T : Message
{
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
        if (options == null)
        {
            options = new JsonSerializerOptions
            {
#if NET8_0_OR_GREATER
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#else
                IgnoreNullValues = true,
#endif
            };

#if NET8_0_OR_GREATER
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
#pragma warning disable IL3050
                options.Converters.Add(new JsonStringEnumConverter());
#pragma warning restore IL3050
            }
#else
            options.Converters.Add(new JsonStringEnumConverter());
#endif
        }

        _options = options;
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
            if (obj.Value.TryGetStringProperty("Type", out var dataType)
                && obj.Value.TryGetStringProperty("Value", out var dataValue))
            {
                var isString = dataType == "String";

                attributes.Add(obj.Name, new MessageAttributeValue()
                {
                    DataType = dataType,
                    StringValue = isString ? dataValue : null,
                    BinaryValue = !isString ? Convert.FromBase64String(dataValue) : null
                });
            }
        }

        return new MessageAttributes(attributes);
    }

    /// <inheritdoc />
    public Message Deserialize(string message, Type type)
    {
        using var document = JsonDocument.Parse(message);
        JsonElement element = document.RootElement.GetProperty("Message");
        string json = element.ToString();

        return DeserializeCore(json, type);
    }

    /// <inheritdoc />
    public string Serialize(Message message, bool serializeForSnsPublishing, string subject)
    {
        string json = SerializeCore(message);

        // AWS SNS service will add Subject and Message properties automatically,
        // so just return plain message
        if (serializeForSnsPublishing)
        {
            return json;
        }

        // For direct publishing to SQS, add Subject and Message properties manually
        var context = new SqsMessageEnvelope { Subject = subject, Message = json };

#if NET8_0_OR_GREATER
        return JsonSerializer.Serialize(context, JustSayingSerializationContext.Default.SqsMessageEnvelope);
#else
        return JsonSerializer.Serialize(context, _options);
#endif
    }

    private Message DeserializeCore(string json, Type type)
    {
#if NET8_0_OR_GREATER
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
#pragma warning disable IL3050
#pragma warning disable IL2026
            return (Message)JsonSerializer.Deserialize(json, type, _options);
#pragma warning restore IL2026
#pragma warning restore IL3050
        }

        var jsonTypeInfo = _options.GetTypeInfo<T>();
        return JsonSerializer.Deserialize(json, jsonTypeInfo);
#else
        return (Message)JsonSerializer.Deserialize(json, type, _options);
#endif
    }

    private string SerializeCore(Message message)
    {
        string json;

#if NET8_0_OR_GREATER
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
#pragma warning disable IL3050
#pragma warning disable IL2026
            json = JsonSerializer.Serialize(message, typeof(T), _options);
#pragma warning restore IL2026
#pragma warning restore IL3050
        }
        else
        {
            var jsonTypeInfo = _options.GetTypeInfo<T>();
            json = JsonSerializer.Serialize(message, jsonTypeInfo);
        }
#else
        json = JsonSerializer.Serialize(message, _options);
#endif

        return json;
    }
}
