using System.Text;
using System.Text.Json;
using JustSaying.Extensions;
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
public sealed class CloudEventEnvelopeBodySerializer<T> : IMessageBodySerializer<CloudEvent<T>>, ISelfDescribingMessageBodySerializer, ICloudEventMessageBodySerializer
    where T : class
{
    private readonly IMessageBodySerializer<T> _dataSerializer;
    private readonly IMessageMetadataProvider _metadataProvider;
    private readonly Uri _source;
    private readonly string _type;
    private readonly string _dataContentType;

    /// <inheritdoc />
    public string Type => _type;

    /// <inheritdoc />
    public Uri Source => _source;

    /// <inheritdoc />
    public string DataContentType => _dataContentType;

    /// <inheritdoc />
    public System.Type DataType => typeof(T);

    /// <inheritdoc />
    public string ContentType => "application/cloudevents+json";

    /// <inheritdoc />
    public IMessageBodyFormat DataFormat => _dataSerializer as IMessageBodyFormat;

    // Built by CloudEventSerializationFactory.GetEnvelopeSerializer. source and type are written only
    // when serializing (publishing); they may be null for a consume-only serializer, which reads them
    // from the inbound envelope instead.
    internal CloudEventEnvelopeBodySerializer(
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
            throw new InvalidOperationException($"A CloudEvents 'type' is required to publish; set CloudEvent<T>.Type or configure MapType<{typeof(T).ToReadableName()}>(...).");
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

        var (id, source, type, time) = CloudEventJsonReader.ReadContext(root, _type);
        var payload = CloudEventJsonReader.ReadData(root, _dataSerializer, typeof(CloudEvent<T>));

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
            time,
            CloudEventJsonReader.GetString(root, "subject"),
            extensions,
            CloudEventJsonReader.GetString(root, "datacontenttype"),
            CloudEventJsonReader.GetUri(root, "dataschema"),
            validateExtensions: false);
    }
}
