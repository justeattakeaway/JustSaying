using System.Text;
using System.Text.Json;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents;

/// <summary>
/// Serializes messages of type <typeparamref name="TMessage"/> as a structured-mode
/// <see href="https://github.com/cloudevents/spec">CloudEvents 1.0</see> JSON envelope, with the
/// message placed under the <c>data</c> member. The envelope is written with
/// <see cref="Utf8JsonWriter"/> (no reflection), so it is Native AOT-safe; the <c>data</c> payload is
/// serialized by an inner <see cref="IMessageBodySerializer{TMessage}"/>.
/// </summary>
/// <typeparam name="TMessage">The type of message to be serialized or deserialized.</typeparam>
public sealed class CloudEventMessageBodySerializer<TMessage> : IMessageBodySerializer<TMessage>, IContextProvidingMessageBodySerializer<TMessage>, ISelfDescribingMessageBodySerializer where TMessage : class
{
    private const string SpecVersion = "1.0";

    private static readonly string[] ReservedAttributes =
    [
        "specversion", "id", "source", "type", "time", "datacontenttype", "dataschema", "subject", "data", "data_base64"
    ];

    private readonly IMessageBodySerializer<TMessage> _dataSerializer;
    private readonly IMessageMetadataProvider _metadataProvider;
    private readonly Uri _source;
    private readonly string _type;
    private readonly string _dataContentType;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventMessageBodySerializer{TMessage}"/> class.
    /// </summary>
    /// <param name="dataSerializer">The serializer used for the <c>data</c> payload.</param>
    /// <param name="metadataProvider">Provides the CloudEvents <c>id</c> and <c>time</c> from the message.</param>
    /// <param name="source">The CloudEvents <c>source</c>.</param>
    /// <param name="type">The CloudEvents <c>type</c> for this message type.</param>
    /// <param name="dataContentType">The CloudEvents <c>datacontenttype</c>. Defaults to <c>application/json</c>.</param>
    public CloudEventMessageBodySerializer(
        IMessageBodySerializer<TMessage> dataSerializer,
        IMessageMetadataProvider metadataProvider,
        Uri source,
        string type,
        string dataContentType = "application/json")
    {
        _dataSerializer = dataSerializer ?? throw new ArgumentNullException(nameof(dataSerializer));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));
        _type = type;
        _dataContentType = dataContentType ?? "application/json";
    }

    /// <summary>
    /// Serializes a message to a structured-mode CloudEvents JSON envelope.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>The CloudEvents JSON.</returns>
    public string Serialize(TMessage message)
    {
        var dataJson = _dataSerializer.Serialize(message);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("specversion", SpecVersion);
            // CloudEvents requires a non-empty id; mint one when the payload carries none.
            writer.WriteString("id", _metadataProvider.GetId(message) ?? Guid.NewGuid().ToString());
            writer.WriteString("source", _source.ToString());
            writer.WriteString("type", _type);
            writer.WriteString("time", _metadataProvider.GetTimestamp(message) ?? DateTimeOffset.UtcNow);
            writer.WriteString("datacontenttype", _dataContentType);

            writer.WritePropertyName("data");
            using (var data = JsonDocument.Parse(dataJson))
            {
                data.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Deserializes the <c>data</c> payload of a structured-mode CloudEvents JSON envelope into a
    /// message of type <typeparamref name="TMessage"/>.
    /// </summary>
    /// <param name="message">The CloudEvents JSON.</param>
    /// <returns>The deserialized message.</returns>
    public TMessage Deserialize(string message)
    {
        using var document = JsonDocument.Parse(message);
        return DeserializeData(document);
    }

    /// <summary>
    /// Deserializes the <c>data</c> payload of a structured-mode CloudEvents JSON envelope into a
    /// message of type <typeparamref name="TMessage"/>, also capturing a factory for a
    /// <see cref="CloudEventMessageContext"/> carrying the envelope's context attributes.
    /// </summary>
    /// <param name="message">The CloudEvents JSON.</param>
    /// <param name="contextFactory">
    /// When this method returns, a factory that creates the <see cref="CloudEventMessageContext"/>
    /// for this message.
    /// </param>
    /// <returns>The deserialized message.</returns>
    public TMessage Deserialize(string message, out MessageContextFactory contextFactory)
    {
        using var document = JsonDocument.Parse(message);
        var data = DeserializeData(document);

        var root = document.RootElement;
        string specVersion = GetStringAttribute(root, "specversion") ?? SpecVersion;
        string id = GetStringAttribute(root, "id");
        string source = GetStringAttribute(root, "source");
        string type = GetStringAttribute(root, "type");
        string dataContentType = GetStringAttribute(root, "datacontenttype");
        string dataSchema = GetStringAttribute(root, "dataschema");
        string subject = GetStringAttribute(root, "subject");

        if (id is null || source is null || type is null)
        {
            // Not a well-formed CloudEvent (id, source and type are required attributes);
            // fall back to the default message context.
            contextFactory = null;
            return data;
        }

        DateTimeOffset? time = null;
        if (root.TryGetProperty("time", out var timeElement) &&
            timeElement.ValueKind == JsonValueKind.String &&
            timeElement.TryGetDateTimeOffset(out var parsedTime))
        {
            time = parsedTime;
        }

        Dictionary<string, string> extensions = null;
        foreach (var property in root.EnumerateObject())
        {
            if (Array.IndexOf(ReservedAttributes, property.Name) >= 0)
            {
                continue;
            }

            // CloudEvents extension attributes are simple (non-composite) values; ignore anything else.
            string value = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.GetRawText(),
                _ => null,
            };

            if (value is not null)
            {
                (extensions ??= new Dictionary<string, string>()).Add(property.Name, value);
            }
        }

        contextFactory = (rawMessage, queueUri, messageAttributes) => new CloudEventMessageContext(
            rawMessage,
            queueUri,
            messageAttributes,
            specVersion,
            id,
            new Uri(source, UriKind.RelativeOrAbsolute),
            type,
            time,
            dataContentType,
            dataSchema is null ? null : new Uri(dataSchema, UriKind.RelativeOrAbsolute),
            subject,
            extensions);

        return data;
    }

    private TMessage DeserializeData(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            throw new InvalidOperationException("The CloudEvents payload does not contain a 'data' member.");
        }

        return _dataSerializer.Deserialize(data.GetRawText());
    }

    private static string GetStringAttribute(JsonElement root, string name)
        => root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
