namespace JustSaying.CloudEvents;

/// <summary>
/// Configures how JustSaying produces and consumes CloudEvents.
/// </summary>
public sealed class CloudEventOptions
{
    private readonly Dictionary<Type, string> _typeNames = new();

    /// <summary>
    /// Gets or sets the CloudEvents <c>source</c> — a URI-reference identifying the producer of the
    /// events (for example <c>https://orders.example.com</c>). Required.
    /// </summary>
    public Uri Source { get; set; }

    /// <summary>
    /// Gets or sets the CloudEvents <c>datacontenttype</c> describing the <c>data</c> payload.
    /// Defaults to <c>application/json</c>.
    /// </summary>
    public string DataContentType { get; set; } = "application/json";

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

    /// <summary>
    /// Gets the CloudEvents <c>type</c> attribute registered for a message type, if any.
    /// </summary>
    /// <param name="messageType">The message type to look up.</param>
    /// <param name="type">The registered CloudEvents <c>type</c>, or <see langword="null"/> if none is registered.</param>
    /// <returns><see langword="true"/> if a CloudEvents <c>type</c> is registered for <paramref name="messageType"/>.</returns>
    public bool TryGetCloudEventType(Type messageType, out string type) => _typeNames.TryGetValue(messageType, out type);
}
