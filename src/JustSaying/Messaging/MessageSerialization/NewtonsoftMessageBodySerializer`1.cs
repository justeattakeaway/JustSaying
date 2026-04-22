using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Provides serialization and deserialization functionality for messages of type <typeparamref name="T"/> using Newtonsoft.Json.
/// </summary>
/// <typeparam name="T">The type of message to be serialized or deserialized. Must inherit from <see cref="Message"/>.</typeparam>
public sealed class NewtonsoftMessageBodySerializer<T> : IMessageBodySerializer where T: Message
{
#if NET8_0_OR_GREATER
    private const string NewtonsoftRequiresUnreferencedCodeMessage = "Newtonsoft.Json relies on reflection over types that may be removed when trimming.";
    private const string NewtonsoftRequiresDynamicCodeMessage = "Newtonsoft.Json relies on dynamically creating types that may not be available with Native AOT.";
#endif

    private readonly JsonSerializerSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftMessageBodySerializer{T}"/> class with default JSON serializer settings.
    /// </summary>
    /// <remarks>
    /// Default settings include:
    /// <list type="bullet">
    /// <item><description>Ignoring null values when serializing.</description></item>
    /// <item><description>Using a <see cref="Newtonsoft.Json.Converters.StringEnumConverter"/> for enum serialization.</description></item>
    /// </list>
    /// </remarks>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode(NewtonsoftRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(NewtonsoftRequiresDynamicCodeMessage)]
#endif
    public NewtonsoftMessageBodySerializer()
    {
        _settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = [new Newtonsoft.Json.Converters.StringEnumConverter()]
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftMessageBodySerializer{T}"/> class with custom JSON serializer settings.
    /// </summary>
    /// <param name="settings">The custom <see cref="JsonSerializerSettings"/> to use for serialization and deserialization.</param>
    public NewtonsoftMessageBodySerializer(JsonSerializerSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Serializes a message to its JSON string representation.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A JSON string representation of the message.</returns>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Caller has accepted Newtonsoft's trimming requirements by selecting this serializer.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Caller has accepted Newtonsoft's dynamic code requirements by selecting this serializer.")]
#endif
    public string Serialize(Message message)
    {
        return JsonConvert.SerializeObject(message, _settings);
    }

    /// <summary>
    /// Deserializes a JSON string to a message of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="message">The JSON string to deserialize.</param>
    /// <returns>A deserialized message of type <typeparamref name="T"/>.</returns>
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Caller has accepted Newtonsoft's trimming requirements by selecting this serializer.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Caller has accepted Newtonsoft's dynamic code requirements by selecting this serializer.")]
#endif
    public Message Deserialize(string message)
    {
        return JsonConvert.DeserializeObject<T>(message, _settings);
    }
}
