using JustSaying.CloudEvents;

namespace JustSaying.Fluent;

/// <summary>
/// CloudEvents extensions for <see cref="SubscriptionsBuilder"/>.
/// </summary>
public static class SubscriptionsBuilderCloudEventExtensions
{
    /// <summary>
    /// Subscribes to a topic of structured-mode CloudEvents of type <typeparamref name="T"/>, with a
    /// handler that receives the full <see cref="CloudEvent{T}"/> envelope (metadata and extension
    /// attributes) — the subscribe-side counterpart of <c>WithCloudEventTopic&lt;T&gt;</c>. Register a
    /// handler for <c>CloudEvent&lt;T&gt;</c>. The topic and queue are named after the payload type
    /// <typeparamref name="T"/> (not the <see cref="CloudEvent{T}"/> wrapper) by the naming conventions,
    /// so the subscription meets a <c>WithCloudEventTopic&lt;T&gt;</c> publication by default.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="subscriptions">The subscriptions builder.</param>
    /// <param name="type">
    /// The CloudEvents <c>type</c> this subscription reads. An event of any other <c>type</c>, or one
    /// that isn't a valid CloudEvent, fails handling (and is retried, then dead-lettered).
    /// </param>
    /// <param name="configure">
    /// An optional delegate to configure the subscription, as for <c>ForTopic&lt;T&gt;</c> (topic and queue
    /// names, filter policy, raw delivery, middleware, …).
    /// </param>
    /// <returns>The current <see cref="SubscriptionsBuilder"/>.</returns>
    public static SubscriptionsBuilder ForCloudEventTopic<T>(
        this SubscriptionsBuilder subscriptions,
        string type,
        Action<TopicSubscriptionBuilder<CloudEvent<T>>> configure = null)
        where T : class
    {
        if (subscriptions is null) throw new ArgumentNullException(nameof(subscriptions));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        return subscriptions.ForTopic<CloudEvent<T>>(builder =>
        {
            builder.TopicNameResolver = convention => convention.TopicName<T>();
            builder.QueueNameResolver = convention => convention.QueueName<T>();
            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetEnvelopeSerializer<T>(type);

            configure?.Invoke(builder);
        });
    }

    /// <summary>
    /// Subscribes to a topic of structured-mode CloudEvents of type <typeparamref name="T"/>, with a
    /// handler that receives the bare <c>data</c> payload — the envelope is stripped before dispatch,
    /// so the handler is a plain <c>IHandlerAsync&lt;T&gt;</c>. The topic and queue are named after
    /// <typeparamref name="T"/> by the naming conventions, so the subscription meets a
    /// <c>WithCloudEventTopic&lt;T&gt;</c> publication by default. Use
    /// <see cref="ForCloudEventTopic{T}"/> instead when the handler wants the envelope.
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload the handler receives.</typeparam>
    /// <param name="subscriptions">The subscriptions builder.</param>
    /// <param name="type">The CloudEvents <c>type</c> this subscription reads.</param>
    /// <param name="configure">
    /// An optional delegate to configure the subscription, as for <c>ForTopic&lt;T&gt;</c> (topic and queue
    /// names, filter policy, raw delivery, middleware, …).
    /// </param>
    /// <returns>The current <see cref="SubscriptionsBuilder"/>.</returns>
    public static SubscriptionsBuilder ForCloudEventTopicData<T>(
        this SubscriptionsBuilder subscriptions,
        string type,
        Action<TopicSubscriptionBuilder<T>> configure = null)
        where T : class
    {
        if (subscriptions is null) throw new ArgumentNullException(nameof(subscriptions));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        return subscriptions.ForTopic<T>(builder =>
        {
            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetDataSerializer<T>(type);

            configure?.Invoke(builder);
        });
    }
}
