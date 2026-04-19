using System.Text.Json;
using JustSaying.Models;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
using JustSaying.Extensions;
#endif

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Provides serialization and deserialization functionality for messages of type <typeparamref name="T"/> using System.Text.Json.
/// </summary>
/// <typeparam name="T">The type of message to be serialized or deserialized. Must inherit from <see cref="Message"/>.</typeparam>
public sealed class SystemTextJsonMessageBodySerializer<T> : IMessageBodySerializer where T: Message
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonMessageBodySerializer{T}"/> class with default JSON serializer options.
    /// </summary>
    public SystemTextJsonMessageBodySerializer() : this(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
    { }

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
#if NET8_0_OR_GREATER
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
#pragma warning disable IL3050
#pragma warning disable IL2026
            return JsonSerializer.Serialize(message, message.GetType(), _options);
#pragma warning restore IL2026
#pragma warning restore IL3050
        }

        var jsonTypeInfo = _options.GetTypeInfo<T>();
        return JsonSerializer.Serialize((T)message, jsonTypeInfo);
#else
        return JsonSerializer.Serialize(message, message.GetType(), _options);
#endif
    }

    /// <summary>
    /// Deserializes a JSON string to a message of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="messageBody">The JSON string to deserialize.</param>
    /// <returns>A deserialized message of type <typeparamref name="T"/>.</returns>
    public Message Deserialize(string messageBody)
    {
#if NET8_0_OR_GREATER
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
#pragma warning disable IL3050
#pragma warning disable IL2026
            return JsonSerializer.Deserialize<T>(messageBody, _options);
#pragma warning restore IL2026
#pragma warning restore IL3050
        }

        var jsonTypeInfo = _options.GetTypeInfo<T>();
        return JsonSerializer.Deserialize(messageBody, jsonTypeInfo);
#else
        return JsonSerializer.Deserialize<T>(messageBody, _options);
#endif
    }
}
