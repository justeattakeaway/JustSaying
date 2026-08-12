using JustSaying.Extensions;
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
    private readonly IMessageMetadataProvider _bareMessageMetadataProvider;
    private readonly CloudEventOptions _options;

    // Built by AddJustSayingCloudEvents, which supplies the data factory and metadata provider.
    internal CloudEventSerializationFactory(
        IMessageBodySerializationFactory dataSerializerFactory,
        IMessageMetadataProvider metadataProvider,
        CloudEventOptions options)
    {
        _dataSerializerFactory = dataSerializerFactory ?? throw new ArgumentNullException(nameof(dataSerializerFactory));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));

        // A bare payload published as a CloudEvent gets an id and time minted once per message
        // instance, so they don't change between the publish attempts of one PublishAsync call. (The
        // CloudEvent<T> envelope serializer mints its own, per envelope instance.)
        _bareMessageMetadataProvider = MintedEventIdentity.Fallback(_metadataProvider);
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Source and the type map are outbound-only concerns, so they are not required here — a
        // consume-only application reads them from the inbound envelope. They are validated when a
        // message is actually serialized for publishing.
    }

    /// <summary>
    /// Gets the factory whose serializers handle the CloudEvents <c>data</c> payload,
    /// which describes the wire contract of the payload, for example for schema generation.
    /// </summary>
    public IMessageBodySerializationFactory DataSerializerFactory => _dataSerializerFactory;

    /// <inheritdoc />
    public IMessageBodySerializer<TMessage> GetSerializer<TMessage>() where TMessage : class
    {
        var messageType = typeof(TMessage);
        if (messageType.IsGenericType && messageType.GetGenericTypeDefinition() == typeof(CloudEvent<>))
        {
            // A plain registration of the envelope (ForTopic<CloudEvent<T>>, WithTopic<CloudEvent<T>>) in
            // an app with UseAsDefault. This factory reads and writes bare payloads, so name the
            // registrations that handle the envelope, keyed on the payload type.
            var data = messageType.GetGenericArguments()[0].ToReadableName();
            throw new InvalidOperationException(
                $"'{messageType.ToReadableName()}' can't use the app-wide CloudEvents serializer, which reads and writes bare payloads. " +
                $"Register it with ForCloudEventTopic<{data}>(...), HandlingCloudEvent<{data}>(...) or WithCloudEventTopic<{data}>(...) instead, " +
                $"and map its CloudEvents 'type' with {nameof(CloudEventOptions)}.{nameof(CloudEventOptions.MapType)}<{data}>(\"...\") or at the registration.");
        }

        if (!_options.TryGetCloudEventType(messageType, out var type))
        {
            throw new InvalidOperationException(
                $"No CloudEvents 'type' is configured for message type '{messageType.ToReadableFullName()}'. " +
                $"Configure one via {nameof(CloudEventOptions)}.{nameof(CloudEventOptions.MapType)}<{messageType.ToReadableName()}>(\"...\").");
        }

        // This is the app-default path (UseAsDefault), which serves subscriptions as well as
        // publications. A consumer never needs the `source`, so a missing one fails at the first publish
        // (in the serializer) rather than here.
        var dataSerializer = _dataSerializerFactory.GetSerializer<TMessage>();
        return new CloudEventMessageBodySerializer<TMessage>(dataSerializer, _bareMessageMetadataProvider, _options.Source, type, _options.DataContentType);
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
    internal IMessageBodySerializer<T> GetSerializer<T>(string type, Uri source = null) where T : class
    {
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        var resolvedSource = source ?? _options.Source
            ?? throw new InvalidOperationException(
                $"A CloudEvents 'source' is required to publish '{typeof(T).ToReadableFullName()}' as a bare message. " +
                $"Pass source: to the WithCloudEventTopic<{typeof(T).ToReadableName()}>/WithCloudEventQueue<{typeof(T).ToReadableName()}> registration, " +
                $"set {nameof(CloudEventOptions)}.{nameof(CloudEventOptions.Source)}, " +
                $"or publish a CloudEvent<{typeof(T).ToReadableName()}> with its Source set.");

        var dataSerializer = _dataSerializerFactory.GetSerializer<T>();
        return new CloudEventMessageBodySerializer<T>(dataSerializer, _bareMessageMetadataProvider, resolvedSource, type, _options.DataContentType);
    }

    /// <summary>
    /// Gets a serializer that deserializes a structured-mode CloudEvents envelope into its bare
    /// <c>data</c> payload (the envelope is stripped), without requiring any outbound configuration —
    /// used by a subscription that consumes a CloudEvent as plain <typeparamref name="T"/> and states
    /// the <c>type</c> at the subscription. Serializing with it requires a <c>source</c> and
    /// <c>type</c> (from <paramref name="type"/> / the options), like any CloudEvents publication.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="type">
    /// The CloudEvents <c>type</c> this serializer reads and writes, or <see langword="null"/> to fall
    /// back to the type configured via <see cref="CloudEventOptions.MapType{TMessage}"/>. With a type,
    /// an event of any other type fails to deserialize; with none at all (it is only needed to publish),
    /// an event of any type is read.
    /// </param>
    public IMessageBodySerializer<T> GetDataOnlySerializer<T>(string type = null) where T : class
    {
        type ??= TryGetCloudEventType<T>();
        var dataSerializer = _dataSerializerFactory.GetSerializer<T>();
        return new CloudEventMessageBodySerializer<T>(dataSerializer, _bareMessageMetadataProvider, _options.Source, type, _options.DataContentType);
    }

    /// <summary>
    /// Gets a serializer that deserializes the structured-mode CloudEvents envelope into a
    /// <see cref="CloudEvent{T}"/>, preserving the envelope metadata (rather than just the <c>data</c>
    /// payload). Used by handlers that opt into the envelope.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="type">
    /// The CloudEvents <c>type</c> this serializer reads and writes, or <see langword="null"/> to fall
    /// back to the type configured via <see cref="CloudEventOptions.MapType{TMessage}"/>. With a type,
    /// an event of any other type fails to deserialize; with none at all (it is only needed to publish),
    /// an event of any type is read.
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
    /// <see cref="CloudEventOptions.MapType{TMessage}"/>), throwing if none is configured.
    /// Used as the fallback routing key for a subscription that did not state the <c>type</c> itself.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    internal string GetCloudEventType<T>() where T : class
        => TryGetCloudEventType<T>()
           ?? throw new InvalidOperationException(
               $"No CloudEvents 'type' is configured for message type '{typeof(T).ToReadableFullName()}'. " +
               $"Pass it to HandlingCloudEvent<{typeof(T).ToReadableName()}>(\"...\"), or configure one via " +
               $"{nameof(CloudEventOptions)}.{nameof(CloudEventOptions.MapType)}<{typeof(T).ToReadableName()}>(\"...\").");

    private string TryGetCloudEventType<T>() where T : class
        => _options.TryGetCloudEventType(typeof(T), out var type) ? type : null;
}
