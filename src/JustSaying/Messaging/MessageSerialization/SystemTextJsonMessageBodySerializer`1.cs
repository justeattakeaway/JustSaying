using System.Text.Json;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JustSaying.Extensions;
#endif

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Provides serialization and deserialization functionality for messages of type <typeparamref name="T"/> using System.Text.Json.
/// </summary>
/// <typeparam name="T">The type of message to be serialized or deserialized.</typeparam>
public sealed class SystemTextJsonMessageBodySerializer<T> : IMessageBodySerializer<T> where T : class
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTextJsonMessageBodySerializer{T}"/> class with default JSON serializer options.
    /// </summary>
    /// <remarks>
    /// The default options have no <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver"/>, so under Native AOT
    /// the resulting serializer throws <see cref="NotSupportedException"/> on first use. Use the
    /// <see cref="SystemTextJsonMessageBodySerializer{T}(JsonSerializerOptions)"/> overload with a source-generated context to
    /// remain AOT-compatible.
    /// </remarks>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("The default JsonSerializerOptions have no TypeInfoResolver, so serialization falls back to reflection over message types that may be removed when trimming.")]
    [RequiresDynamicCode("The default JsonSerializerOptions have no TypeInfoResolver, so serialization falls back to reflection-based metadata that requires dynamic code.")]
#endif
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
    /// Serializes a message to its JSON string representation, by its declared type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A JSON string representation of the message.</returns>
    public string Serialize(T message)
    {
#if NET8_0_OR_GREATER
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
#pragma warning disable IL2026, IL3050
            return JsonSerializer.Serialize(message, _options);
#pragma warning restore IL2026, IL3050
        }

        return JsonSerializer.Serialize(message, _options.GetTypeInfo<T>());
#else
        return JsonSerializer.Serialize(message, _options);
#endif
    }

    /// <summary>
    /// Deserializes a JSON string to a message of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="messageBody">The JSON string to deserialize.</param>
    /// <returns>A deserialized message of type <typeparamref name="T"/>.</returns>
    public T Deserialize(string messageBody)
    {
#if NET8_0_OR_GREATER
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
#pragma warning disable IL2026, IL3050
            return JsonSerializer.Deserialize<T>(messageBody, _options);
#pragma warning restore IL2026, IL3050
        }

        return JsonSerializer.Deserialize(messageBody, _options.GetTypeInfo<T>());
#else
        return JsonSerializer.Deserialize<T>(messageBody, _options);
#endif
    }
}
