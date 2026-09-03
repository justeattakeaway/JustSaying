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
    /// <param name="serializer">
    /// The <see cref="MessageSerialization.IMessageBodySerializer{TMessage}"/> (for <paramref name="messageType"/>)
    /// the registration serializes its message bodies with, or <see langword="null"/> when it was not captured.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="messageType"/> is <see langword="null"/>.
    /// </exception>
    public MessageTypeMetadata(Type messageType, string wireName, object serializer = null)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        WireName = wireName;
        Serializer = serializer;
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

    /// <summary>
    /// Gets the serializer the registration serializes its message bodies with — an
    /// <see cref="MessageSerialization.IMessageBodySerializer{TMessage}"/> for <see cref="MessageType"/> —
    /// or <see langword="null"/> when it was not captured. Serialization is configured per registration
    /// (a publication or subscription can override the app-wide default), so the serializer is what
    /// describes the actual wire format of this message: tooling such as AsyncAPI generation inspects it
    /// (for example for <see cref="MessageSerialization.ISystemTextJsonMessageBodySerializer"/>) to
    /// derive the payload's content type and schema.
    /// </summary>
    public object Serializer { get; }
}
