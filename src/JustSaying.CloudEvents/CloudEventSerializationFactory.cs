using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents;

/// <summary>
/// An <see cref="IMessageBodySerializationFactory"/> that produces
/// <see cref="CloudEventMessageBodySerializer{TMessage}"/> instances, wrapping the serializers from an
/// inner factory (used for the CloudEvents <c>data</c> payload).
/// </summary>
public sealed class CloudEventSerializationFactory : IMessageBodySerializationFactory
{
    private readonly IMessageBodySerializationFactory _dataSerializerFactory;
    private readonly IMessageMetadataProvider _metadataProvider;
    private readonly CloudEventOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventSerializationFactory"/> class.
    /// </summary>
    /// <param name="dataSerializerFactory">The factory whose serializers handle the <c>data</c> payload.</param>
    /// <param name="metadataProvider">Provides the CloudEvents <c>id</c> and <c>time</c> from messages.</param>
    /// <param name="options">The CloudEvents options (source, content type and per-type <c>type</c> mappings).</param>
    public CloudEventSerializationFactory(
        IMessageBodySerializationFactory dataSerializerFactory,
        IMessageMetadataProvider metadataProvider,
        CloudEventOptions options)
    {
        _dataSerializerFactory = dataSerializerFactory ?? throw new ArgumentNullException(nameof(dataSerializerFactory));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Source and the type map are outbound-only concerns, so they are not required here — a
        // consume-only application reads them from the inbound envelope. They are validated when a
        // message is actually serialized for publishing.
    }

    /// <inheritdoc />
    public IMessageBodySerializer<TMessage> GetSerializer<TMessage>() where TMessage : class
    {
        if (!_options.TryGetCloudEventType(typeof(TMessage), out var type))
        {
            throw new InvalidOperationException(
                $"No CloudEvents 'type' is configured for message type '{typeof(TMessage).FullName}'. " +
                $"Configure one via {nameof(CloudEventOptions)}.{nameof(CloudEventOptions.WithCloudEventType)}<{typeof(TMessage).Name}>(\"...\").");
        }

        var dataSerializer = _dataSerializerFactory.GetSerializer<TMessage>();
        return new CloudEventMessageBodySerializer<TMessage>(dataSerializer, _metadataProvider, _options.Source, type, _options.DataContentType);
    }

    /// <summary>
    /// Gets a serializer that writes bare <typeparamref name="T"/> messages as structured-mode
    /// CloudEvents with the given <paramref name="type"/> — used by a publication whose CloudEvents
    /// <c>type</c> is stated at the publication rather than in the global type map.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <param name="source">
    /// The CloudEvents <c>source</c> to write, or <see langword="null"/> to fall back to
    /// <see cref="CloudEventOptions.Source"/>. One of the two must be set.
    /// </param>
    /// <exception cref="InvalidOperationException">Neither <paramref name="source"/> nor <see cref="CloudEventOptions.Source"/> is set.</exception>
    public IMessageBodySerializer<T> GetSerializer<T>(string type, Uri source = null) where T : class
    {
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        var resolvedSource = source ?? _options.Source
            ?? throw new InvalidOperationException(
                $"A CloudEvents 'source' is required to publish '{typeof(T).FullName}' as a bare message. " +
                $"Pass source: to WithCloudEvent<{typeof(T).Name}>(...), set {nameof(CloudEventOptions)}.{nameof(CloudEventOptions.Source)}, " +
                $"or publish a CloudEvent<{typeof(T).Name}> with its Source set.");

        var dataSerializer = _dataSerializerFactory.GetSerializer<T>();
        return new CloudEventMessageBodySerializer<T>(dataSerializer, _metadataProvider, resolvedSource, type, _options.DataContentType);
    }

    /// <summary>
    /// Gets a serializer that deserializes the structured-mode CloudEvents envelope into a
    /// <see cref="CloudEvent{T}"/>, preserving the envelope metadata (rather than just the <c>data</c>
    /// payload). Used by handlers that opt into the envelope.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="type">
    /// The CloudEvents <c>type</c> to write when publishing. When <see langword="null"/>, falls back to
    /// the type configured via <see cref="CloudEventOptions.WithCloudEventType{TMessage}"/> (which may
    /// itself be absent — the value is only needed to publish, not to consume).
    /// </param>
    /// <param name="source">
    /// The default CloudEvents <c>source</c> to write when publishing, or <see langword="null"/> to
    /// fall back to <see cref="CloudEventOptions.Source"/>. May remain unset for a consume-only
    /// serializer, or when every published <see cref="CloudEvent{T}"/> sets its own Source.
    /// </param>
    public IMessageBodySerializer<CloudEvent<T>> GetEnvelopeSerializer<T>(string type = null, Uri source = null) where T : class
    {
        type ??= TryGetCloudEventType<T>();
        var dataSerializer = _dataSerializerFactory.GetSerializer<T>();
        return new CloudEventEnvelopeBodySerializer<T>(dataSerializer, _metadataProvider, source ?? _options.Source, type, _options.DataContentType);
    }

    /// <summary>
    /// Gets the configured CloudEvents <c>type</c> for <typeparamref name="T"/> (set via
    /// <see cref="CloudEventOptions.WithCloudEventType{TMessage}"/>), throwing if none is configured.
    /// Used as the fallback routing key for a subscription that did not state the <c>type</c> itself.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public string GetCloudEventType<T>() where T : class
        => TryGetCloudEventType<T>()
           ?? throw new InvalidOperationException(
               $"No CloudEvents 'type' is configured for message type '{typeof(T).FullName}'. " +
               $"Pass it to HandlingCloudEvent<{typeof(T).Name}>(\"...\"), or configure one via " +
               $"{nameof(CloudEventOptions)}.{nameof(CloudEventOptions.WithCloudEventType)}<{typeof(T).Name}>(\"...\").");

    private string TryGetCloudEventType<T>() where T : class
        => _options.TryGetCloudEventType(typeof(T), out var type) ? type : null;
}
