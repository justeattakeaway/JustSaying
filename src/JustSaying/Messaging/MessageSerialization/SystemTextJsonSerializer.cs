using System.Text.Json;
using System.Text.Json.Serialization;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// A class representing an implementation of <see cref="IMessageSerializer"/> for the <c>System.Text.Json</c> serializer.
/// </summary>
public class SystemTextJsonSerializer : IMessageSerializer
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
                IgnoreNullValues = true
            };

            options.Converters.Add(new JsonStringEnumConverter());
        }

        _options = options;
    }

    /// <inheritdoc />
    public string GetMessageSubject(string sqsMessage)
    {
        using (var body = JsonDocument.Parse(sqsMessage))
        {
            string subject = string.Empty;

            if (body.RootElement.TryGetProperty("Subject", out var value))
            {
                subject = value.GetString() ?? string.Empty;
            }

            return subject;
        }
    }

    /// <inheritdoc />
    public Message Deserialize(string message, Type type)
    {
        using (var document = JsonDocument.Parse(message))
        {
            JsonElement element = document.RootElement.GetProperty("Message");
            string json = element.ToString();

            return (Message)JsonSerializer.Deserialize(json, type, _options);
        }
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
