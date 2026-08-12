namespace JustSaying.Messaging.Metadata;

/// <summary>
/// Describes a publication registered with the messaging bus, captured when the
/// fluent builders are configured and before any AWS infrastructure is provisioned.
/// </summary>
public sealed class PublicationMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PublicationMetadata"/> class.
    /// </summary>
    /// <param name="destinationKind">The kind of destination published to.</param>
    /// <param name="destinationName">The resolved destination name, or <see langword="null"/> when the destination is dynamic.</param>
    /// <param name="isDynamic">Whether the destination is computed per-message at publish time.</param>
    /// <param name="messages">The message type(s) published to the destination.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="messages"/> is <see langword="null"/>.
    /// </exception>
    public PublicationMetadata(
        MessagingDestinationKind destinationKind,
        string destinationName,
        bool isDynamic,
        IReadOnlyList<MessageTypeMetadata> messages)
    {
        DestinationKind = destinationKind;
        DestinationName = destinationName;
        IsDynamic = isDynamic;
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
    }

    /// <summary>
    /// Gets the kind of destination published to.
    /// </summary>
    public MessagingDestinationKind DestinationKind { get; }

    /// <summary>
    /// Gets the resolved destination name (topic or queue name), or
    /// <see langword="null"/> when the destination is dynamic.
    /// </summary>
    public string DestinationName { get; }

    /// <summary>
    /// Gets a value indicating whether the destination is computed per-message at
    /// publish time, for example by a topic name customizer.
    /// </summary>
    public bool IsDynamic { get; }

    /// <summary>
    /// Gets the message type(s) published to the destination.
    /// </summary>
    public IReadOnlyList<MessageTypeMetadata> Messages { get; }
}
