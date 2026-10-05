using System.Diagnostics.CodeAnalysis;
using JustSaying.CloudEvents;

namespace JustSaying.Fluent;

/// <summary>
/// CloudEvents extensions for <see cref="PublicationsBuilder"/>.
/// </summary>
/// <remarks>
/// Each registration accepts two publish shapes — the bare <c>T</c> and a <see cref="CloudEvent{T}"/> —
/// and so registers two publications behind the scenes. The optional <c>configure</c> callback configures
/// the bare <c>T</c> publication, and is mirrored onto the <see cref="CloudEvent{T}"/> one: every setting
/// applies to both, and a callback that receives a message (an exception handler, a topic name
/// customizer) receives the envelope's <see cref="CloudEvent{T}.Data"/>.
/// </remarks>
public static class PublicationsBuilderCloudEventExtensions
{
    /// <summary>
    /// Registers a topic publication that writes messages of type <typeparamref name="T"/> as
    /// structured-mode CloudEvents. The CloudEvents <c>type</c> is stated here, co-located with the
    /// publication. Only this publication speaks CloudEvents; other publications keep the app-wide
    /// default serializer.
    /// <para>
    /// Both shapes can then be published: the bare <typeparamref name="T"/> (the envelope's
    /// <c>id</c>/<c>time</c>/<c>source</c> are defaulted), or a <see cref="CloudEvent{T}"/> to set the
    /// <c>source</c>, <c>subject</c> and extension attributes per message. Both go to the same topic.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="publications">The publications builder.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <param name="source">
    /// The CloudEvents <c>source</c> for messages published as the bare <typeparamref name="T"/>, or
    /// <see langword="null"/> to fall back to <c>CloudEventOptions.Source</c>. One of the two must be
    /// set (verified when the bus is built); a published <see cref="CloudEvent{T}"/> can override it
    /// per message.
    /// </param>
    /// <param name="topicName">
    /// The name of the topic to publish to, or <see langword="null"/> (or blank) to name it by the topic
    /// naming convention applied to <typeparamref name="T"/>. Both publish shapes must agree on the
    /// name, so a blank name is treated as "not provided" rather than as an explicit empty name.
    /// </param>
    /// <param name="configure">
    /// An optional delegate to configure the publication (exception handlers, middleware, compression,
    /// …), applied to both publish shapes.
    /// </param>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads",
        Justification = "The TopicDestination overloads deliberately share this name; their leading parameter type disambiguates every call, and the API is unshipped (v9).")]
    public static PublicationsBuilder WithCloudEventTopic<T>(
        this PublicationsBuilder publications,
        string type,
        Uri source = null,
        string topicName = null,
        Action<TopicPublicationBuilder<T>> configure = null)
        where T : class
    {
        if (publications is null) throw new ArgumentNullException(nameof(publications));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        // The bare-model publication: publishing a T writes a CloudEvent with defaulted metadata.
        TopicPublicationBuilder<T> bare = null;
        publications.WithTopic<T>(builder =>
        {
            if (!string.IsNullOrWhiteSpace(topicName))
            {
                builder.WithTopicName(topicName);
            }

            configure?.Invoke(builder);
            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetSerializer<T>(type, source);
            bare = builder;
        });

        // The envelope publication: publishing a CloudEvent<T> controls the metadata per message.
        // Same topic — named after the payload type T, not the CloudEvent<T> wrapper — and the SNS
        // Subject reflects the payload type too.
        publications.WithTopic<CloudEvent<T>>(builder => ConfigureEnvelope(bare, builder, type, source));

        return publications;
    }

    /// <summary>
    /// Registers a point-to-point queue publication that writes messages of type
    /// <typeparamref name="T"/> as structured-mode CloudEvents — the queue counterpart of
    /// <see cref="WithCloudEventTopic{T}(PublicationsBuilder, string, Uri, string, Action{TopicPublicationBuilder{T}})"/>.
    /// The CloudEvents <c>type</c> is stated here, co-located with the publication; only this
    /// publication speaks CloudEvents. The CloudEvents serializer is self-describing, so the envelope
    /// is written to the queue verbatim — never wrapped in JustSaying's <c>{ "Subject", "Message" }</c>
    /// queue envelope.
    /// <para>
    /// Both shapes can then be published: the bare <typeparamref name="T"/> (the envelope's
    /// <c>id</c>/<c>time</c>/<c>source</c> are defaulted), or a <see cref="CloudEvent{T}"/> to set the
    /// <c>source</c>, <c>subject</c> and extension attributes per message. Both go to the same queue.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="publications">The publications builder.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <param name="source">
    /// The CloudEvents <c>source</c> for messages published as the bare <typeparamref name="T"/>, or
    /// <see langword="null"/> to fall back to <c>CloudEventOptions.Source</c>. One of the two must be
    /// set (verified when the bus is built); a published <see cref="CloudEvent{T}"/> can override it
    /// per message.
    /// </param>
    /// <param name="queueName">
    /// The name of the queue to publish to, or <see langword="null"/> (or blank) to name it by the queue
    /// naming convention applied to <typeparamref name="T"/>. Both publish shapes must agree on the
    /// name, so a blank name is treated as "not provided" rather than as an explicit empty name.
    /// </param>
    /// <param name="configure">
    /// An optional delegate to configure the publication (middleware, compression, …), applied to both
    /// publish shapes.
    /// </param>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0027:API with optional parameter(s) should have the most parameters amongst its public overloads",
        Justification = "The QueueDestination overloads deliberately share this name; their leading parameter type disambiguates every call, and the API is unshipped (v9).")]
    public static PublicationsBuilder WithCloudEventQueue<T>(
        this PublicationsBuilder publications,
        string type,
        Uri source = null,
        string queueName = null,
        Action<QueuePublicationBuilder<T>> configure = null)
        where T : class
    {
        if (publications is null) throw new ArgumentNullException(nameof(publications));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        // The bare-model publication: publishing a T writes a CloudEvent with defaulted metadata.
        QueuePublicationBuilder<T> bare = null;
        publications.WithQueue<T>(builder =>
        {
            if (!string.IsNullOrWhiteSpace(queueName))
            {
                builder.WithQueueName(queueName);
            }

            configure?.Invoke(builder);
            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetSerializer<T>(type, source);
            bare = builder;
        });

        // The envelope publication: publishing a CloudEvent<T> controls the metadata per message.
        // Same queue — named after the payload type T, not the CloudEvent<T> wrapper.
        publications.WithQueue<CloudEvent<T>>(builder => ConfigureEnvelope(bare, builder, type, source));

        return publications;
    }

    /// <summary>
    /// Registers a publication that writes messages of type <typeparamref name="T"/> as
    /// structured-mode CloudEvents to a topic described by a <see cref="TopicDestination"/> destination — named,
    /// by convention, or a pre-existing topic by ARN. Both publish shapes are accepted, as with
    /// <see cref="WithCloudEventTopic{T}(PublicationsBuilder, string, Uri, string, Action{TopicPublicationBuilder{T}})"/>. The
    /// CloudEvents <c>source</c> falls back to <c>CloudEventOptions.Source</c> (one of the two must
    /// be set to publish the bare <typeparamref name="T"/>, verified when the bus is built).
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="publications">The publications builder.</param>
    /// <param name="destination">The topic to publish to.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    public static PublicationsBuilder WithCloudEventTopic<T>(
        this PublicationsBuilder publications,
        TopicDestination destination,
        string type)
        where T : class
        => publications.WithCloudEventTopic<T>(destination, type, null, null);

    /// <inheritdoc cref="WithCloudEventTopic{T}(PublicationsBuilder, TopicDestination, string)"/>
    /// <param name="publications">The publications builder.</param>
    /// <param name="destination">The topic to publish to.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <param name="source">The CloudEvents <c>source</c> for messages published as the bare <typeparamref name="T"/>.</param>
    public static PublicationsBuilder WithCloudEventTopic<T>(
        this PublicationsBuilder publications,
        TopicDestination destination,
        string type,
        Uri source)
        where T : class
        => publications.WithCloudEventTopic<T>(destination, type, source, null);

    /// <inheritdoc cref="WithCloudEventTopic{T}(PublicationsBuilder, TopicDestination, string)"/>
    /// <param name="publications">The publications builder.</param>
    /// <param name="destination">The topic to publish to.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <param name="source">The CloudEvents <c>source</c> for messages published as the bare <typeparamref name="T"/>.</param>
    /// <param name="configure">
    /// A delegate to configure the publication (exception handlers, middleware, compression, …),
    /// applied to both publish shapes.
    /// </param>
    public static PublicationsBuilder WithCloudEventTopic<T>(
        this PublicationsBuilder publications,
        TopicDestination destination,
        string type,
        Uri source,
        Action<TopicPublicationBuilder<T>> configure)
        where T : class
    {
        if (publications is null) throw new ArgumentNullException(nameof(publications));
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        TopicPublicationBuilder<T> bare = null;
        publications.WithTopic<T>(destination, builder =>
        {
            configure?.Invoke(builder);
            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetSerializer<T>(type, source);
            bare = builder;
        });

        // A destination named by convention is resolved per registration, so the envelope would
        // otherwise be named after CloudEvent<T> and both shapes would no longer share a topic. An
        // explicit name or an ARN ignores the envelope's name resolver.
        publications.WithTopic<CloudEvent<T>>(destination, builder => ConfigureEnvelope(bare, builder, type, source));

        return publications;
    }

    /// <summary>
    /// Registers a publication that writes messages of type <typeparamref name="T"/> as
    /// structured-mode CloudEvents to a queue described by a <see cref="QueueDestination"/> destination — named,
    /// by convention, or a pre-existing queue by URL or ARN. Both publish shapes are accepted, as with
    /// <see cref="WithCloudEventQueue{T}(PublicationsBuilder, string, Uri, string, Action{QueuePublicationBuilder{T}})"/>, and the
    /// envelope is the queue body verbatim (the CloudEvents serializer is self-describing). The
    /// CloudEvents <c>source</c> falls back to <c>CloudEventOptions.Source</c> (one of the two must
    /// be set to publish the bare <typeparamref name="T"/>, verified when the bus is built).
    /// </summary>
    /// <typeparam name="T">The type of the <c>data</c> payload.</typeparam>
    /// <param name="publications">The publications builder.</param>
    /// <param name="destination">The queue to publish to.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    public static PublicationsBuilder WithCloudEventQueue<T>(
        this PublicationsBuilder publications,
        QueueDestination destination,
        string type)
        where T : class
        => publications.WithCloudEventQueue<T>(destination, type, null, null);

    /// <inheritdoc cref="WithCloudEventQueue{T}(PublicationsBuilder, QueueDestination, string)"/>
    /// <param name="publications">The publications builder.</param>
    /// <param name="destination">The queue to publish to.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <param name="source">The CloudEvents <c>source</c> for messages published as the bare <typeparamref name="T"/>.</param>
    public static PublicationsBuilder WithCloudEventQueue<T>(
        this PublicationsBuilder publications,
        QueueDestination destination,
        string type,
        Uri source)
        where T : class
        => publications.WithCloudEventQueue<T>(destination, type, source, null);

    /// <inheritdoc cref="WithCloudEventQueue{T}(PublicationsBuilder, QueueDestination, string)"/>
    /// <param name="publications">The publications builder.</param>
    /// <param name="destination">The queue to publish to.</param>
    /// <param name="type">The CloudEvents <c>type</c> written for this message.</param>
    /// <param name="source">The CloudEvents <c>source</c> for messages published as the bare <typeparamref name="T"/>.</param>
    /// <param name="configure">
    /// A delegate to configure the publication (middleware, compression, …), applied to both publish
    /// shapes.
    /// </param>
    public static PublicationsBuilder WithCloudEventQueue<T>(
        this PublicationsBuilder publications,
        QueueDestination destination,
        string type,
        Uri source,
        Action<QueuePublicationBuilder<T>> configure)
        where T : class
    {
        if (publications is null) throw new ArgumentNullException(nameof(publications));
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        QueuePublicationBuilder<T> bare = null;
        publications.WithQueue<T>(destination, builder =>
        {
            configure?.Invoke(builder);
            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetSerializer<T>(type, source);
            bare = builder;
        });

        // As for topics: a convention-named destination is resolved per registration, so the envelope
        // is named after T. An explicit name, a URL or an ARN ignores the envelope's name resolver.
        publications.WithQueue<CloudEvent<T>>(destination, builder => ConfigureEnvelope(bare, builder, type, source));

        return publications;
    }

    private static void ConfigureEnvelope<T>(TopicPublicationBuilder<T> bare, TopicPublicationBuilder<CloudEvent<T>> envelope, string type, Uri source)
        where T : class
    {
        bare.MirrorTo(envelope, cloudEvent => cloudEvent.Data);

        // Applied only when no name is set (by the destination, topicName or the configure callback).
        envelope.TopicNameResolver = convention => convention.TopicName<T>();
        envelope.SubjectResolver = registry => registry.GetLogicalName(typeof(T));
        envelope.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetEnvelopeSerializer<T>(type, source);
    }

    private static void ConfigureEnvelope<T>(QueuePublicationBuilder<T> bare, QueuePublicationBuilder<CloudEvent<T>> envelope, string type, Uri source)
        where T : class
    {
        bare.MirrorTo(envelope);

        // Applied only when no name is set (by the destination, queueName or the configure callback).
        envelope.QueueNameResolver = convention => convention.QueueName<T>();
        envelope.SubjectResolver = registry => registry.GetLogicalName(typeof(T));
        envelope.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetEnvelopeSerializer<T>(type, source);
    }
}
