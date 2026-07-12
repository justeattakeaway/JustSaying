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
    public CloudEvent(
        T data,
        string id = null,
        Uri source = null,
        string type = null,
        DateTimeOffset? time = null,
        string subject = null,
        IReadOnlyDictionary<string, string> extensions = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Id = id;
        Source = source;
        Type = type;
        Time = time;
        Subject = subject;
        Extensions = extensions ?? EmptyExtensions;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyExtensions =
        new Dictionary<string, string>(StringComparer.Ordinal);

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
    /// Gets the CloudEvents extension attributes — any envelope members beyond the spec-defined ones
    /// (for example <c>tenantid</c>, <c>partitionkey</c>, <c>traceparent</c>), as strings.
    /// </summary>
    public IReadOnlyDictionary<string, string> Extensions { get; }
}
