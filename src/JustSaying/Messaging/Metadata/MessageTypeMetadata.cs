namespace JustSaying.Messaging.Metadata;

/// <summary>
/// Describes a message type that flows through a publication or subscription.
/// </summary>
public sealed class MessageTypeMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTypeMetadata"/> class.
    /// </summary>
    /// <param name="messageType">The CLR type of the message.</param>
    /// <param name="wireName">The logical name used to identify the message on the wire.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="messageType"/> is <see langword="null"/>.
    /// </exception>
    public MessageTypeMetadata(Type messageType, string wireName)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        WireName = wireName;
    }

    /// <summary>
    /// Gets the CLR type of the message.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Gets the logical name used to identify the message on the wire, such as
    /// the message subject or an explicitly registered type name.
    /// </summary>
    public string WireName { get; }
}
