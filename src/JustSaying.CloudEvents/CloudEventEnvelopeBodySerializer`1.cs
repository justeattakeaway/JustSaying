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
    private const string SpecVersion = "1.0";

    // The CloudEvents 1.0 spec-defined context attributes; everything else at the top level is an extension.
    private static readonly HashSet<string> ReservedAttributes = new(StringComparer.Ordinal)
    {
        "specversion", "id", "source", "type", "time", "datacontenttype", "dataschema", "subject", "data", "data_base64",
    };

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
    public string Serialize(CloudEvent<T> message)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));

        var source = message.Source ?? _source
            ?? throw new InvalidOperationException("A CloudEvents 'source' is required to publish; set CloudEvent<T>.Source or CloudEventOptions.Source.");
        var type = message.Type ?? _type
            ?? throw new InvalidOperationException($"A CloudEvents 'type' is required to publish; set CloudEvent<T>.Type or configure WithCloudEventType<{typeof(T).Name}>(...).");

        var dataJson = _dataSerializer.Serialize(message.Data);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("specversion", SpecVersion);
            writer.WriteString("id", message.Id ?? _metadataProvider.GetId(message.Data) ?? Guid.NewGuid().ToString());
            writer.WriteString("source", source.ToString());
            writer.WriteString("type", type);
            writer.WriteString("time", message.Time ?? _metadataProvider.GetTimestamp(message.Data) ?? DateTimeOffset.UtcNow);
            writer.WriteString("datacontenttype", _dataContentType);

            if (!string.IsNullOrEmpty(message.Subject))
            {
                writer.WriteString("subject", message.Subject);
            }

            foreach (var extension in message.Extensions)
            {
                if (!ReservedAttributes.Contains(extension.Key))
                {
                    writer.WriteString(extension.Key, extension.Value);
                }
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
    public CloudEvent<T> Deserialize(string message)
    {
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;

        if (!root.TryGetProperty("data", out var data))
        {
            throw new InvalidOperationException("The CloudEvents payload does not contain a 'data' member.");
        }

        var payload = _dataSerializer.Deserialize(data.GetRawText());

        Dictionary<string, string> extensions = null;
        foreach (var member in root.EnumerateObject())
        {
            if (ReservedAttributes.Contains(member.Name) || member.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            (extensions ??= new Dictionary<string, string>(StringComparer.Ordinal))[member.Name] = member.Value.GetString();
        }

        return new CloudEvent<T>(
            payload,
            id: GetString(root, "id"),
            source: TryGetUri(root, "source"),
            type: GetString(root, "type"),
            time: TryGetTime(root),
            subject: GetString(root, "subject"),
            extensions: extensions);
    }

    private static string GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static Uri TryGetUri(JsonElement root, string name)
        => GetString(root, name) is { Length: > 0 } s && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;

    private static DateTimeOffset? TryGetTime(JsonElement root)
        => root.TryGetProperty("time", out var v) && v.ValueKind == JsonValueKind.String && v.TryGetDateTimeOffset(out var t) ? t : null;
}
