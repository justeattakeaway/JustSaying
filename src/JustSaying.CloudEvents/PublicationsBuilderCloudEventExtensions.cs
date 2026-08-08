using JustSaying.CloudEvents;

namespace JustSaying.Fluent;

/// <summary>
/// CloudEvents extensions for <see cref="PublicationsBuilder"/>.
/// </summary>
public static class PublicationsBuilderCloudEventExtensions
{
    /// <summary>
    /// Registers a topic publication that writes messages of type <typeparamref name="T"/> as
    /// structured-mode CloudEvents — the publish-side counterpart of <c>HandlingCloudEvent&lt;T&gt;</c>.
    /// The CloudEvents <c>type</c> is stated here, co-located with the publication. Only this
    /// publication speaks CloudEvents; other publications keep the app-wide default serializer.
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
    /// The name of the topic to publish to, or <see langword="null"/> to name it by the topic naming
    /// convention applied to <typeparamref name="T"/>.
    /// </param>
    /// <returns>The current <see cref="PublicationsBuilder"/>.</returns>
    public static PublicationsBuilder WithCloudEvent<T>(
        this PublicationsBuilder publications,
        string type,
        Uri source = null,
        string topicName = null)
        where T : class
    {
        if (publications is null) throw new ArgumentNullException(nameof(publications));
        if (string.IsNullOrEmpty(type)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(type));

        // The bare-model publication: publishing a T writes a CloudEvent with defaulted metadata.
        publications.WithTopic<T>(builder =>
        {
            if (topicName is not null)
            {
                builder.WithTopicName(topicName);
            }

            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetSerializer<T>(type, source);
        });

        // The envelope publication: publishing a CloudEvent<T> controls the metadata per message.
        // Same topic — named after the payload type T, not the CloudEvent<T> wrapper — and the SNS
        // Subject reflects the payload type too.
        publications.WithTopic<CloudEvent<T>>(builder =>
        {
            if (topicName is not null)
            {
                builder.WithTopicName(topicName);
            }
            else
            {
                builder.TopicNameResolver = convention => convention.TopicName<T>();
            }

            builder.SubjectResolver = registry => registry.GetLogicalName(typeof(T));
            builder.SerializerOverride = resolver => resolver.ResolveCloudEventSerializationFactory().GetEnvelopeSerializer<T>(type, source);
        });

        return publications;
    }
}
