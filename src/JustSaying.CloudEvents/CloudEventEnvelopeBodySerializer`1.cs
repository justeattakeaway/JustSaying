using System.Text;
using System.Text.Json;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents;

/// <summary>
/// Serializes and deserializes a <see cref="CloudEvent{T}"/> as a structured-mode CloudEvents 1.0 JSON
/// envelope, preserving the envelope metadata (<c>source</c>, <c>id</c>, <c>time</c>, <c>subject</c> and
/// extension attributes) so a handler can receive it. The envelope is written with
/// <see cref="Utf8JsonWriter"/> (no reflection), so it is Native AOT-safe; the <c>data</c> payload is
/// handled by an inner <see cref="IMessageBodySerializer{TMessage}"/>.
/// </summary>
/// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
public sealed class CloudEventEnvelopeBodySerializer<T> : IMessageBodySerializer<CloudEvent<T>>, ISelfDescribingMessageBodySerializer
    where T : class
{
    private readonly IMessageBodySerializer<T> _dataSerializer;
    private readonly IMessageMetadataProvider _metadataProvider;
    private readonly Uri _source;
    private readonly string _type;
    private readonly string _dataContentType;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventEnvelopeBodySerializer{T}"/> class.
    /// <paramref name="source"/> and <paramref name="type"/> are written only when serializing
    /// (publishing); they may be <see langword="null"/> for a consume-only serializer, which reads them
    /// from the inbound envelope instead.
    /// </summary>
    public CloudEventEnvelopeBodySerializer(
        IMessageBodySerializer<T> dataSerializer,
        IMessageMetadataProvider metadataProvider,
        Uri source = null,
        string type = null,
        string dataContentType = "application/json")
    {
        _dataSerializer = dataSerializer ?? throw new ArgumentNullException(nameof(dataSerializer));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _source = source;
        _type = type;
        _dataContentType = dataContentType ?? "application/json";
    }

    /// <summary>Serializes a <see cref="CloudEvent{T}"/> to a structured-mode CloudEvents JSON envelope.</summary>
    /// <exception cref="InvalidOperationException">No <c>source</c> or <c>type</c> is set on the event or the serializer.</exception>
    /// <exception cref="ArgumentException">An extension attribute name isn't valid in CloudEvents 1.0.</exception>
    public string Serialize(CloudEvent<T> message)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));

        var source = message.Source ?? _source;
        if (string.IsNullOrEmpty(source?.OriginalString))
        {
            throw new InvalidOperationException("A CloudEvents 'source' is required to publish; set CloudEvent<T>.Source or CloudEventOptions.Source.");
        }

        var type = string.IsNullOrEmpty(message.Type) ? _type : message.Type;
        if (string.IsNullOrEmpty(type))
        {
            throw new InvalidOperationException($"A CloudEvents 'type' is required to publish; set CloudEvent<T>.Type or configure MapType<{typeof(T).Name}>(...).");
        }

        // An event read from another producer keeps its extensions verbatim, so the names are checked
        // here too: one invalid name makes the whole event unreadable to other CloudEvents consumers.
        CloudEventAttributes.ValidateExtensionNames(message.Extensions, nameof(message));

        var dataJson = _dataSerializer.Serialize(message.Data);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("specversion", CloudEventAttributes.SpecVersion);
            // An id and time minted here are kept for this CloudEvent<T> instance, so every publish
            // attempt (the bus serializes again per retry) writes the same ones.
            writer.WriteString("id", string.IsNullOrEmpty(message.Id) ? _metadataProvider.GetId(message.Data) ?? MintedEventIdentity.For(message).Id : message.Id);

            // OriginalString, not ToString(): ToString() normalizes the URI (lowercasing the host, adding
            // a trailing slash) and unescapes it (%20 becomes a space), so the value would change.
            writer.WriteString("source", source.OriginalString);
            writer.WriteString("type", type);
            writer.WriteString("time", message.Time ?? _metadataProvider.GetTimestamp(message.Data) ?? MintedEventIdentity.For(message).Time);
            writer.WriteString("datacontenttype", string.IsNullOrEmpty(message.DataContentType) ? _dataContentType : message.DataContentType);

            if (message.DataSchema is not null)
            {
                writer.WriteString("dataschema", message.DataSchema.OriginalString);
            }

            if (!string.IsNullOrEmpty(message.Subject))
            {
                writer.WriteString("subject", message.Subject);
            }

            foreach (var extension in message.Extensions)
            {
                writer.WriteString(extension.Key, extension.Value);
            }

            writer.WritePropertyName("data");
            using (var data = JsonDocument.Parse(dataJson))
            {
                data.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Deserializes a structured-mode CloudEvents JSON envelope into a <see cref="CloudEvent{T}"/>.</summary>
    /// <exception cref="InvalidOperationException">
    /// The message isn't a valid CloudEvents 1.0 event (a required attribute is missing or malformed, or
    /// the <c>specversion</c> isn't <c>1.0</c>), its <c>type</c> isn't the one this serializer reads, or
    /// it carries no data. The message then fails handling (and is retried, then dead-lettered) rather
    /// than reaching a handler half-populated.
    /// </exception>
    public CloudEvent<T> Deserialize(string message)
    {
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("The message is not a structured-mode CloudEvent: expected a JSON object.");
        }

        var specVersion = GetString(root, "specversion");
        if (specVersion != CloudEventAttributes.SpecVersion)
        {
            throw new InvalidOperationException(
                $"The CloudEvent has specversion '{specVersion ?? "<missing>"}'; only CloudEvents {CloudEventAttributes.SpecVersion} is supported.");
        }

        var id = GetRequiredString(root, "id");
        var source = GetUri(root, "source")
            ?? throw new InvalidOperationException("The CloudEvent has no valid 'source' attribute, which CloudEvents requires.");
        var type = GetRequiredString(root, "type");

        if (_type is not null && !string.Equals(type, _type, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The CloudEvent has type '{type}', but this subscription reads CloudEvents of type '{_type}'.");
        }

        var dataContentType = GetString(root, "datacontenttype");
        var payload = DeserializeData(root, dataContentType);

        Dictionary<string, string> extensions = null;
        foreach (var member in root.EnumerateObject())
        {
            if (CloudEventAttributes.Reserved.Contains(member.Name))
            {
                continue;
            }

            // A CloudEvents attribute value is a string, an integer or a boolean; a null is an unset
            // attribute, and an object or array isn't a valid attribute value.
            var value = member.Value.ValueKind switch
            {
                JsonValueKind.String => member.Value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => member.Value.GetRawText(),
                _ => null,
            };

            if (value is not null)
            {
                (extensions ??= new Dictionary<string, string>(StringComparer.Ordinal))[member.Name] = value;
            }
        }

        return new CloudEvent<T>(
            payload,
            id,
            source,
            type,
            GetTime(root),
            GetString(root, "subject"),
            extensions,
            dataContentType,
            GetUri(root, "dataschema"),
            validateExtensions: false);
    }

    private T DeserializeData(JsonElement root, string dataContentType)
    {
        var hasData = root.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null;
        var hasBase64Data = root.TryGetProperty("data_base64", out var base64Data) && base64Data.ValueKind != JsonValueKind.Null;

        if (hasData && hasBase64Data)
        {
            throw new InvalidOperationException("The CloudEvent has both 'data' and 'data_base64' members, which CloudEvents forbids.");
        }

        string dataJson;
        if (hasData)
        {
            dataJson = data.GetRawText();
        }
        else if (hasBase64Data)
        {
            // Binary data travels base64-encoded; JSON data sent that way is decoded and read like inline
            // data. Any other media type can't be read into a typed payload.
            if (base64Data.ValueKind != JsonValueKind.String || !CloudEventAttributes.IsJsonContentType(dataContentType))
            {
                throw new InvalidOperationException(
                    $"The CloudEvent carries 'data_base64' of content type '{dataContentType}'; only JSON data can be read into {typeof(T).Name}.");
            }

            dataJson = Encoding.UTF8.GetString(base64Data.GetBytesFromBase64());
        }
        else
        {
            // A data-less event is valid CloudEvents, but CloudEvent<T>.Data is never null, so that a
            // handler can rely on it; such an event fails handling instead.
            throw new InvalidOperationException(
                $"The CloudEvent has no data, but CloudEvent<{typeof(T).Name}> requires a {typeof(T).Name} payload.");
        }

        return _dataSerializer.Deserialize(dataJson)
               ?? throw new InvalidOperationException(
                   $"The CloudEvent's data deserialized to null, but CloudEvent<{typeof(T).Name}> requires a {typeof(T).Name} payload.");
    }

    private static string GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string GetRequiredString(JsonElement root, string name)
        => GetString(root, name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"The CloudEvent has no '{name}' attribute, which CloudEvents requires.");

    private static Uri GetUri(JsonElement root, string name)
        => GetString(root, name) is { Length: > 0 } s && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;

    private static DateTimeOffset? GetTime(JsonElement root)
    {
        if (!root.TryGetProperty("time", out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String && CloudEventAttributes.TryParseTime(value.GetString(), out var time))
        {
            return time;
        }

        throw new InvalidOperationException(
            $"The CloudEvent's 'time' attribute {value.GetRawText()} is not an RFC 3339 timestamp with a time zone offset.");
    }
}
