using System.Collections.ObjectModel;

namespace JustSaying.CloudEvents;

/// <summary>
/// A CloudEvents 1.0 envelope around a strongly-typed <c>data</c> payload. Handle
/// <see cref="CloudEvent{T}"/> instead of <typeparamref name="T"/> to receive the envelope metadata
/// (<c>source</c>, <c>id</c>, <c>time</c>, <c>subject</c> and extension attributes) alongside the data,
/// rather than just the deserialized payload.
/// </summary>
/// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
public sealed class CloudEvent<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEvent{T}"/> class. <paramref name="id"/>,
    /// <paramref name="source"/> and <paramref name="type"/> are normally supplied by the serializer
    /// (or, when publishing, defaulted from configuration); set them explicitly to control the envelope.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// An extension attribute name isn't valid in CloudEvents 1.0: it must consist of lowercase letters
    /// (<c>a</c>–<c>z</c>) and digits only, and must not be a spec-defined attribute name.
    /// </exception>
    public CloudEvent(
        T data,
        string id = null,
        Uri source = null,
        string type = null,
        DateTimeOffset? time = null,
        string subject = null,
        IReadOnlyDictionary<string, string> extensions = null,
        string dataContentType = null,
        Uri dataSchema = null)
        : this(data, id, source, type, time, subject, extensions, dataContentType, dataSchema, validateExtensions: true)
    {
    }

    // An inbound event's extensions are preserved as the producer wrote them, even when a name isn't
    // valid, rather than failing to read the event; republishing it is what rejects the bad name.
    internal CloudEvent(
        T data,
        string id,
        Uri source,
        string type,
        DateTimeOffset? time,
        string subject,
        IReadOnlyDictionary<string, string> extensions,
        string dataContentType,
        Uri dataSchema,
        bool validateExtensions)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Id = id;
        Source = source;
        Type = type;
        Time = time;
        Subject = subject;
        DataContentType = dataContentType;
        DataSchema = dataSchema;
        Extensions = extensions ?? EmptyExtensions;

        if (validateExtensions)
        {
            CloudEventAttributes.ValidateExtensionNames(Extensions, nameof(extensions));
        }
    }

    // Shared across every instance created without extensions, so it has to be genuinely read-only:
    // a bare Dictionary could be cast back and mutated, leaking data between unrelated messages.
    private static readonly IReadOnlyDictionary<string, string> EmptyExtensions =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>Gets the deserialized <c>data</c> payload.</summary>
    public T Data { get; }

    /// <summary>Gets the CloudEvents <c>id</c>.</summary>
    public string Id { get; }

    /// <summary>Gets the CloudEvents <c>source</c>.</summary>
    public Uri Source { get; }

    /// <summary>Gets the CloudEvents <c>type</c>.</summary>
    public string Type { get; }

    /// <summary>Gets the CloudEvents <c>time</c>, if present.</summary>
    public DateTimeOffset? Time { get; }

    /// <summary>Gets the CloudEvents <c>subject</c>, if present.</summary>
    public string Subject { get; }

    /// <summary>
    /// Gets the CloudEvents <c>datacontenttype</c>, if present. When publishing, <see langword="null"/>
    /// uses the registration's content type (<c>application/json</c> by default).
    /// </summary>
    public string DataContentType { get; }

    /// <summary>Gets the CloudEvents <c>dataschema</c>, if present.</summary>
    public Uri DataSchema { get; }

    /// <summary>
    /// Gets the CloudEvents extension attributes — any envelope members beyond the spec-defined ones
    /// (for example <c>tenantid</c>, <c>partitionkey</c>, <c>traceparent</c>), as strings. An inbound
    /// integer or boolean extension is held as its JSON text (<c>42</c>, <c>true</c>) and is written
    /// back as a string.
    /// </summary>
    public IReadOnlyDictionary<string, string> Extensions { get; }
}
