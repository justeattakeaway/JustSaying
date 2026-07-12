using JustSaying.CloudEvents;
using JustSaying.Messaging.MessageSerialization;
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
    /// automatically (once), so the inbound message is routed by its CloudEvents <c>type</c>.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="builder">The multi-type subscription builder.</param>
    /// <param name="typeName">
    /// The CloudEvents <c>type</c> the discriminator matches for this message. When <see langword="null"/>
    /// (the default), it is derived from the type configured via
    /// <see cref="CloudEventOptions.WithCloudEventType{TMessage}"/> — so the <c>type</c> is named once.
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
            serializerFactory: factory => Require(factory).GetEnvelopeSerializer<T>(typeName),
            typeNameResolver: factory => Require(factory).GetCloudEventType<T>(),
            middlewareConfiguration: middlewareConfiguration);
    }

    private static CloudEventSerializationFactory Require(IMessageBodySerializationFactory factory)
        => factory as CloudEventSerializationFactory
           ?? throw new InvalidOperationException(
               $"CloudEvents envelope handling requires the CloudEvents serialization factory; call AddJustSayingCloudEvents(...). Found '{factory?.GetType().Name ?? "null"}'.");
}
