using System.Text;
using System.Text.Json;
using JustSaying.Messaging;
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
public sealed class CloudEventMessageBodySerializer<TMessage> : IMessageBodySerializer<TMessage>, ISelfDescribingMessageBodySerializer where TMessage : class
{
    private const string SpecVersion = "1.0";

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
    /// <param name="dataContentType">
    /// The CloudEvents <c>datacontenttype</c>, which must be a JSON media type. Defaults to <c>application/json</c>.
    /// </param>
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
        if (string.IsNullOrEmpty(source.OriginalString)) throw new ArgumentException("Parameter cannot be an empty URI.", nameof(source));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));
        _type = type;
        _dataContentType = dataContentType ?? "application/json";
        if (!JsonMediaType.IsJson(_dataContentType))
        {
            throw new ArgumentException($"The CloudEvents datacontenttype must be a JSON media type (such as application/json), but was '{_dataContentType}'.", nameof(dataContentType));
        }
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
            var id = _metadataProvider.GetId(message);
            writer.WriteString("id", string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id);
            // As configured: Uri.ToString() would normalize it (lowercasing the host, adding a trailing
            // slash) and unescape it, which can leave an invalid URI-reference.
            writer.WriteString("source", _source.OriginalString);
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
    /// message of type <typeparamref name="TMessage"/>. The event must be a CloudEvents 1.0 event of
    /// this serializer's <c>type</c> with a non-null <c>data</c>, or a <c>data_base64</c> holding JSON.
    /// </summary>
    /// <param name="message">The CloudEvents JSON.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidOperationException">The event can't be read as a <typeparamref name="TMessage"/>.</exception>
    public TMessage Deserialize(string message)
    {
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("The CloudEvents payload is not a JSON object.");
        }

        var specVersion = GetString(root, "specversion");
        if (specVersion != SpecVersion)
        {
            throw new InvalidOperationException(
                $"The CloudEvent has specversion '{specVersion ?? "<none>"}', but only CloudEvents {SpecVersion} is supported.");
        }

        var type = GetString(root, "type");
        if (type != _type)
        {
            throw new InvalidOperationException(
                $"The CloudEvent has type '{type ?? "<none>"}', but this serializer reads '{_type}' events as '{typeof(TMessage).Name}'.");
        }

        var hasData = root.TryGetProperty("data", out var data);
        var hasDataBase64 = root.TryGetProperty("data_base64", out var dataBase64);

        if (hasData && hasDataBase64)
        {
            throw new InvalidOperationException("The CloudEvent has both 'data' and 'data_base64', which is not allowed.");
        }

        string dataJson;
        if (hasData && data.ValueKind != JsonValueKind.Null)
        {
            dataJson = data.GetRawText();
        }
        else if (hasDataBase64 && dataBase64.ValueKind == JsonValueKind.String)
        {
            // Binary data; only JSON can be read into a message, so the content type must be JSON (or absent).
            var dataContentType = GetString(root, "datacontenttype");
            if (dataContentType is not null && !JsonMediaType.IsJson(dataContentType))
            {
                throw new InvalidOperationException(
                    $"The CloudEvent's 'data_base64' has datacontenttype '{dataContentType}', but only JSON data can be read as '{typeof(TMessage).Name}'.");
            }

            dataJson = Encoding.UTF8.GetString(dataBase64.GetBytesFromBase64());
        }
        else
        {
            throw new InvalidOperationException(
                $"The CloudEvent of type '{type}' has no data, so it can't be read as '{typeof(TMessage).Name}'.");
        }

        return _dataSerializer.Deserialize(dataJson)
            ?? throw new InvalidOperationException($"The CloudEvent of type '{type}' has data that deserialized to null.");
    }

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
