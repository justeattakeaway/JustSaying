using System.Text.Json;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Provides serialization and deserialization functionality for messages of type <typeparamref name="T"/> using System.Text.Json.
/// </summary>
/// <typeparam name="T">The type of message to be serialized or deserialized. Must inherit from <see cref="Message"/>.</typeparam>
public sealed class SystemTextJsonMessageBodySerializer<T> : IMessageBodySerializer where T: Message
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonMessageBodySerializer{T}"/> class with custom JSON serializer options.
    /// </summary>
    /// <param name="options">The custom <see cref="JsonSerializerOptions"/> to use for serialization and deserialization.</param>
    public SystemTextJsonMessageBodySerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Serializes a message to its JSON string representation.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A JSON string representation of the message.</returns>
    public string Serialize(Message message)
    {
        return JsonSerializer.Serialize(message, message.GetType(), _options);
    }

    /// <summary>
    /// Deserializes a JSON string to a message of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="messageBody">The JSON string to deserialize.</param>
    /// <returns>A deserialized message of type <typeparamref name="T"/>.</returns>
    public Message Deserialize(string messageBody)
    {
        return JsonSerializer.Deserialize<T>(messageBody, _options);
    }
}
