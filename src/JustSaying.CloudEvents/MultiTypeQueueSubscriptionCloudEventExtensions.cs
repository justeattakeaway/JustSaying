using JustSaying.CloudEvents;
using JustSaying.Messaging.Middleware;

namespace JustSaying.Fluent;

/// <summary>
/// CloudEvents extensions for <see cref="MultiTypeQueueSubscriptionBuilder"/>.
/// </summary>
public static class MultiTypeQueueSubscriptionCloudEventExtensions
{
    /// <summary>
    /// Registers a message type on a multi-type queue whose handler receives the full
    /// <see cref="CloudEvent{T}"/> envelope (metadata and extension attributes) rather than just the
    /// <c>data</c> payload. Register a handler for <c>CloudEvent&lt;T&gt;</c>; a
    /// <see cref="CloudEventTypeDiscriminator"/> is added to the queue's discriminator chain
    /// automatically (once), so the inbound message is routed by its CloudEvents <c>type</c>. Other
    /// message types on the same queue are unaffected — they keep their own serializers, so native
    /// JustSaying messages and CloudEvents can share a queue.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="builder">The multi-type subscription builder.</param>
    /// <param name="typeName">
    /// The CloudEvents <c>type</c> the discriminator matches for this message. When <see langword="null"/>
    /// (the default), it is derived from the type configured via
    /// <see cref="CloudEventOptions.MapType{TMessage}"/> — so the <c>type</c> is named once.
    /// Pass an explicit value only to match a <c>type</c> produced by another system.
    /// </param>
    /// <param name="middlewareConfiguration">An optional middleware configuration for this type's handler.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public static MultiTypeQueueSubscriptionBuilder HandlingCloudEvent<T>(
        this MultiTypeQueueSubscriptionBuilder builder,
        string typeName = null,
        Action<HandlerMiddlewareBuilder> middlewareConfiguration = null)
        where T : class
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));

        // Resolving an inbound CloudEvent by its `type` requires the CloudEvents discriminator; add it
        // for the user (idempotently, so it composes with any explicitly-configured discriminators).
        builder.EnsureDiscriminator(static () => new CloudEventTypeDiscriminator());

        return builder.Handling<CloudEvent<T>>(
            typeName,
            serializerFactory: resolver => resolver.ResolveCloudEventSerializationFactory().GetEnvelopeSerializer<T>(typeName),
            typeNameResolver: resolver => resolver.ResolveCloudEventSerializationFactory().GetCloudEventType<T>(),
            middlewareConfiguration: middlewareConfiguration);
    }

    /// <summary>
    /// Registers a message type on a multi-type queue that arrives as a structured-mode CloudEvent but
    /// is handled as its bare <c>data</c> payload — the envelope is stripped before dispatch, so the
    /// handler is a plain <c>IHandlerAsync&lt;T&gt;</c> with no CloudEvents in its contract. The
    /// CloudEvents <c>type</c> stated here does double duty: it routes the inbound message (a
    /// <see cref="CloudEventTypeDiscriminator"/> is added to the chain automatically, once) and selects
    /// this registration's serializer — no entry in the <see cref="CloudEventOptions"/> type map is
    /// needed. Use <c>HandlingCloudEvent&lt;T&gt;</c> instead when the handler wants the envelope.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload the handler receives.</typeparam>
    /// <param name="builder">The multi-type subscription builder.</param>
    /// <param name="typeName">
    /// The CloudEvents <c>type</c> the discriminator matches for this message. When <see langword="null"/>
    /// (the default), it is derived from the type configured via
    /// <see cref="CloudEventOptions.MapType{TMessage}"/>.
    /// </param>
    /// <param name="middlewareConfiguration">An optional middleware configuration for this type's handler.</param>
    /// <returns>The current <see cref="MultiTypeQueueSubscriptionBuilder"/>.</returns>
    public static MultiTypeQueueSubscriptionBuilder HandlingCloudEventData<T>(
        this MultiTypeQueueSubscriptionBuilder builder,
        string typeName = null,
        Action<HandlerMiddlewareBuilder> middlewareConfiguration = null)
        where T : class
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));

        builder.EnsureDiscriminator(static () => new CloudEventTypeDiscriminator());

        return builder.Handling<T>(
            typeName,
            serializerFactory: resolver => resolver.ResolveCloudEventSerializationFactory().GetDataSerializer<T>(typeName),
            typeNameResolver: resolver => resolver.ResolveCloudEventSerializationFactory().GetCloudEventType<T>(),
            middlewareConfiguration: middlewareConfiguration);
    }
}
