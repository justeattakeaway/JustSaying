using System.Text;
using System.Text.Json;
using JustSaying.Extensions;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents;

/// <summary>
/// Reads a structured-mode CloudEvents 1.0 JSON event for both serializers, so the bare payload and the
/// <see cref="CloudEvent{T}"/> envelope accept and reject exactly the same events.
/// </summary>
internal static class CloudEventJsonReader
{
    /// <summary>
    /// Reads and validates the required context attributes: the event must be a JSON object with
    /// <c>specversion</c> <c>1.0</c>, a non-empty <c>id</c> and <c>type</c>, a <c>source</c> that is a
    /// URI-reference and, when present, an RFC 3339 <c>time</c>. When <paramref name="expectedType"/> is
    /// set, the event must also be of that <c>type</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event isn't a valid CloudEvents 1.0 event of the expected type.</exception>
    public static (string Id, Uri Source, string Type, DateTimeOffset? Time) ReadContext(JsonElement root, string expectedType)
    {
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

        if (expectedType is not null && !string.Equals(type, expectedType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The CloudEvent has type '{type}', but this subscription reads CloudEvents of type '{expectedType}'.");
        }

        return (id, source, type, GetTime(root));
    }

    /// <summary>
    /// Reads the event's <c>data</c> (or JSON <c>data_base64</c>) with <paramref name="dataSerializer"/>.
    /// </summary>
    /// <param name="root">The event.</param>
    /// <param name="dataSerializer">The serializer for the payload.</param>
    /// <param name="target">The type being read, for errors: the payload or its <see cref="CloudEvent{T}"/>.</param>
    /// <exception cref="InvalidOperationException">The event has no data, or none that can be read as JSON.</exception>
    public static T ReadData<T>(JsonElement root, IMessageBodySerializer<T> dataSerializer, Type target)
        where T : class
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
            var dataContentType = GetString(root, "datacontenttype");
            if (base64Data.ValueKind != JsonValueKind.String || !CloudEventAttributes.IsJsonContentType(dataContentType))
            {
                throw new InvalidOperationException(
                    $"The CloudEvent carries 'data_base64' of content type '{dataContentType}'; only JSON data can be read as {target.ToReadableName()}.");
            }

            dataJson = Encoding.UTF8.GetString(base64Data.GetBytesFromBase64());
        }
        else
        {
            // A data-less event is valid CloudEvents, but neither a handler's payload nor
            // CloudEvent<T>.Data is ever null, so that a handler can rely on it; such an event fails
            // handling instead.
            throw new InvalidOperationException(
                $"The CloudEvent has no data, but {target.ToReadableName()} requires a {typeof(T).ToReadableName()} payload.");
        }

        return dataSerializer.Deserialize(dataJson)
               ?? throw new InvalidOperationException(
                   $"The CloudEvent's data deserialized to null, but {target.ToReadableName()} requires a {typeof(T).ToReadableName()} payload.");
    }

    public static string GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    public static Uri GetUri(JsonElement root, string name)
        => GetString(root, name) is { Length: > 0 } s && Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;

    private static string GetRequiredString(JsonElement root, string name)
        => GetString(root, name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"The CloudEvent has no '{name}' attribute, which CloudEvents requires.");

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
