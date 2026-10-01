using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents;

/// <summary>
/// Configures how JustSaying produces and consumes CloudEvents.
/// </summary>
public sealed class CloudEventOptions
{
    private readonly Dictionary<Type, string> _typeNames = new();
    private string _dataContentType = "application/json";

    /// <summary>
    /// Gets or sets the CloudEvents <c>source</c> — a URI-reference identifying the producer of the
    /// events (for example <c>/orders</c>). Required to publish a bare payload unless the publication
    /// states its own; a consume-only application can leave it unset. Prefer a relative reference:
    /// some consumers (such as AWS.Messaging) reject an absolute URI.
    /// </summary>
    public Uri Source { get; set; }

    /// <summary>
    /// Gets or sets the CloudEvents <c>datacontenttype</c> describing the <c>data</c> payload.
    /// Defaults to <c>application/json</c>. The <c>data</c> is always written as JSON, so this must be a
    /// JSON media type: <c>*/json</c> or one with a <c>+json</c> suffix.
    /// </summary>
    /// <exception cref="ArgumentException">The value is not a JSON media type.</exception>
    public string DataContentType
    {
        get => _dataContentType;
        set => _dataContentType = JsonMediaType.IsJson(value)
            ? value
            : throw new ArgumentException($"The CloudEvents datacontenttype must be a JSON media type (such as application/json), but was '{value}'.", nameof(value));
    }

    /// <summary>
    /// Gets or sets the serialization factory for the <c>data</c> payload. When <see langword="null"/>
    /// (the default), the application's own <see cref="IMessageBodySerializationFactory"/> is used — the
    /// one registered for its other messages — so the <c>data</c> is written with the same JSON settings,
    /// and a source-generated <c>JsonSerializerContext</c> registered once for Native AOT covers it too.
    /// With no factory registered, System.Text.Json defaults are used.
    /// </summary>
    public IMessageBodySerializationFactory DataSerializationFactory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether CloudEvents is also the application-wide default
    /// serialization format, so every plain registration (<c>WithTopic&lt;T&gt;</c>,
    /// <c>ForQueue&lt;T&gt;</c>, …) speaks CloudEvents too — for an all-CloudEvents application. Every
    /// published type must then have a <c>type</c> mapped via <see cref="MapType{TMessage}"/> (an
    /// unmapped type fails at startup rather than silently publishing plain JSON). The default is
    /// <see langword="false"/>: only the CloudEvents registrations (<c>WithCloudEventTopic&lt;T&gt;</c>,
    /// <c>HandlingCloudEvent&lt;T&gt;</c>, …) speak CloudEvents.
    /// </summary>
    public bool UseAsDefault { get; set; }

    /// <summary>
    /// Maps a message type to its CloudEvents <c>type</c> attribute. The CloudEvents specification
    /// recommends a reverse-DNS value (for example <c>com.example.orders.order.placed</c>).
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="type">The CloudEvents <c>type</c> to use for <typeparamref name="TMessage"/>.</param>
    /// <returns>The same <see cref="CloudEventOptions"/> instance, for chaining.</returns>
    public CloudEventOptions MapType<TMessage>(string type) where TMessage : class
    {
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        _typeNames[typeof(TMessage)] = type;
        return this;
    }

    internal bool TryGetCloudEventType(Type messageType, out string type) => _typeNames.TryGetValue(messageType, out type);
}
