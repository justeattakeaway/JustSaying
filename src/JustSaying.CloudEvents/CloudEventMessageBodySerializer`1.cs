using System.Text;
using System.Text.Json;
using JustSaying.Extensions;
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
public sealed class CloudEventMessageBodySerializer<TMessage> : IMessageBodySerializer<TMessage>, ISelfDescribingMessageBodySerializer, ICloudEventMessageBodySerializer where TMessage : class
{
    private readonly IMessageBodySerializer<TMessage> _dataSerializer;
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
    public System.Type DataType => typeof(TMessage);

    /// <inheritdoc />
    public string ContentType => "application/cloudevents+json";

    /// <inheritdoc />
    public IMessageBodyFormat DataFormat => _dataSerializer as IMessageBodyFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventMessageBodySerializer{TMessage}"/> class.
    /// <paramref name="source"/> and <paramref name="type"/> are written only when serializing
    /// (publishing); they may be <see langword="null"/> for a consume-only serializer, which unwraps
    /// the inbound envelope's <c>data</c> without needing either.
    /// </summary>
    /// <param name="dataSerializer">The serializer used for the <c>data</c> payload.</param>
    /// <param name="metadataProvider">Provides the CloudEvents <c>id</c> and <c>time</c> from the message.</param>
    /// <param name="source">The CloudEvents <c>source</c>, required to serialize.</param>
    /// <param name="type">The CloudEvents <c>type</c> for this message type, required to serialize.</param>
    /// <param name="dataContentType">
    /// The CloudEvents <c>datacontenttype</c>, which must be a JSON media type. Defaults to <c>application/json</c>.
    /// </param>
    internal CloudEventMessageBodySerializer(
        IMessageBodySerializer<TMessage> dataSerializer,
        IMessageMetadataProvider metadataProvider,
        Uri source = null,
        string type = null,
        string dataContentType = "application/json")
    {
        _dataSerializer = dataSerializer ?? throw new ArgumentNullException(nameof(dataSerializer));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _source = source;
        if (source is not null && string.IsNullOrEmpty(source.OriginalString)) throw new ArgumentException("Parameter cannot be an empty URI.", nameof(source));
        if (type is not null && type.Length == 0) throw new ArgumentException("Parameter cannot be empty.", nameof(type));
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
        var source = _source
            ?? throw new InvalidOperationException($"A CloudEvents 'source' is required to publish '{typeof(TMessage).ToReadableFullName()}'; set CloudEventOptions.Source or pass one at the publication registration.");
        var type = _type
            ?? throw new InvalidOperationException($"A CloudEvents 'type' is required to publish '{typeof(TMessage).ToReadableFullName()}'; configure MapType<{typeof(TMessage).ToReadableName()}>(...) or pass one at the publication registration.");

        var dataJson = _dataSerializer.Serialize(message);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("specversion", CloudEventAttributes.SpecVersion);
            // CloudEvents requires a non-empty id; mint one when the payload carries none.
            var id = _metadataProvider.GetId(message);
            writer.WriteString("id", string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id);
            // As configured: Uri.ToString() would normalize it (lowercasing the host, adding a trailing
            // slash) and unescape it, which can leave an invalid URI-reference.
            writer.WriteString("source", source.OriginalString);
            writer.WriteString("type", type);
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
    /// message of type <typeparamref name="TMessage"/>. The event must be a valid CloudEvents 1.0 event
    /// (validated as for a <see cref="CloudEvent{T}"/>) of this serializer's <c>type</c>, when it has one,
    /// with a non-null <c>data</c>, or a <c>data_base64</c> holding JSON.
    /// </summary>
    /// <param name="message">The CloudEvents JSON.</param>
    /// <returns>The deserialized message.</returns>
    /// <exception cref="InvalidOperationException">The event can't be read as a <typeparamref name="TMessage"/>.</exception>
    public TMessage Deserialize(string message)
    {
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;

        // With no type (a consume-only serializer whose type isn't known), an event of any type is read.
        CloudEventJsonReader.ReadContext(root, _type);
        return CloudEventJsonReader.ReadData(root, _dataSerializer, typeof(TMessage));
    }
}
